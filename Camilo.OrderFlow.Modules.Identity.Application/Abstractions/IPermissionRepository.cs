using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default);
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<bool> IsAssignedToAnyRoleAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    void Add(Permission permission);
    void Update(Permission permission);
    void Remove(Permission permission);
    IQueryable<Permission> Query();
}
