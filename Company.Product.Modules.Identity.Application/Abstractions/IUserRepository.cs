using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetPagedAsync(string? search, int skip, int take, CancellationToken ct = default);
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    Task<bool> IsOnlyActiveAdminAsync(Guid userId, CancellationToken ct = default);
    void Add(User user);
    void Update(User user);
    void Remove(User user);
    IQueryable<User> Query();
}
