using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly IdentityDbContext _context;

    public PermissionRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
        => await _context.Permissions.AsNoTracking().ToListAsync(ct);

    public Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> IsAssignedToAnyRoleAsync(Guid id, CancellationToken ct = default)
        => _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id, ct);

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return _context.Permissions.FirstOrDefaultAsync(p => p.Code == normalized, ct);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return _context.Permissions.AnyAsync(p => p.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _context.Permissions.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public void Add(Permission permission) => _context.Permissions.Add(permission);

    public void Update(Permission permission) => _context.Permissions.Update(permission);

    public void Remove(Permission permission) => _context.Permissions.Remove(permission);

    public IQueryable<Permission> Query() => _context.Permissions.AsNoTracking();
}
