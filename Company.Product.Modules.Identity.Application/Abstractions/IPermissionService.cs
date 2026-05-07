namespace Company.Product.Modules.Identity.Application.Abstractions;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
