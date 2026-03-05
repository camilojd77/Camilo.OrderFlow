namespace Company.Product.Modules.Identity.Infrastructure.Authorization;

public interface IPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
