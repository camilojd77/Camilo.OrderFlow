using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Domain.Entities;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => await _context.Roles.AsNoTracking().ToListAsync(ct);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default)
        => _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _context.Roles.FirstOrDefaultAsync(r => r.Name == normalized, ct);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return _context.Roles.AnyAsync(r => r.Name == normalized, ct);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await _context.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(ct);

    public void Add(Role role) => _context.Roles.Add(role);

    public void Update(Role role) => _context.Roles.Update(role);

    public void Remove(Role role) => _context.Roles.Remove(role);

    public IQueryable<Role> Query() => _context.Roles.AsNoTracking();
}
