using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default)
        => _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        return _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.AnyAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken ct = default)
    {
        var normalized = userName.Trim().ToLowerInvariant();
        return _context.Users.AnyAsync(u => u.UserName.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlyList<User>> GetPagedAsync(
        string? search, int skip, int take, CancellationToken ct = default)
    {
        var q = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.Email.Contains(search) || u.UserName.Contains(search));
        return await q.OrderBy(u => u.Email).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<int> CountAsync(string? search, CancellationToken ct = default)
    {
        var q = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.Email.Contains(search) || u.UserName.Contains(search));
        return await q.CountAsync(ct);
    }

    public async Task<bool> IsOnlyActiveAdminAsync(Guid userId, CancellationToken ct = default)
    {
        var activeAdminIds = await _context.UserRoles
            .Where(ur => ur.Role.Name == "Admin" && ur.User.IsActive)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        return activeAdminIds.Count == 1 && activeAdminIds[0] == userId;
    }

    public void Add(User user) => _context.Users.Add(user);

    public void Update(User user) => _context.Users.Update(user);

    public void Remove(User user) => _context.Users.Remove(user);

    public IQueryable<User> Query() => _context.Users.AsNoTracking();
}
