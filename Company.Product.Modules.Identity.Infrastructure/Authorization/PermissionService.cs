using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Company.Product.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionService : IPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IdentityDbContext _context;
    private readonly IMemoryCache _cache;

    public PermissionService(IdentityDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<HashSet<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"perm:{userId}";

        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached is not null)
            return cached;

        var permissions = (await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken));

        var set = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _cache.Set(cacheKey, permissions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration
        });

        return set;
    }
}
