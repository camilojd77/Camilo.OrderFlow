using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Abstractions;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    void Add(Role role);
    void Update(Role role);
    void Remove(Role role);
    IQueryable<Role> Query();
}
