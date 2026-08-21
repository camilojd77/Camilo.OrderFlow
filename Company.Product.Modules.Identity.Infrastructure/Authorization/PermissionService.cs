using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Company.Product.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionService : IPermissionService
{
    private const string UserKeyPrefix = "perms:user:";
    private const string RoleTagPrefix = "role:";

    private readonly IdentityDbContext _context;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _entryOptions;

    public PermissionService(
        IdentityDbContext context,
        HybridCache hybridCache,
        HybridCacheEntryOptions entryOptions)
    {
        _context = context;
        _hybridCache = hybridCache;
        _entryOptions = entryOptions;
    }

    public async Task<HashSet<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Role IDs are needed to build cache tags even on cache hit.
        // This is a lightweight indexed FK lookup.
        var roleIds = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var tags = roleIds.Count > 0
            ? roleIds.ConvertAll(id => RoleTagPrefix + id)
            : null;

        return await _hybridCache.GetOrCreateAsync(
            UserKeyPrefix + userId,
            async ct =>
            {
                var codes = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.UserId == userId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .ToListAsync(ct);
                return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            },
            _entryOptions,
            tags,
            cancellationToken);
    }

    public async Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _hybridCache.RemoveAsync(UserKeyPrefix + userId, cancellationToken);

    public async Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => await _hybridCache.RemoveByTagAsync(RoleTagPrefix + roleId, cancellationToken);
}
