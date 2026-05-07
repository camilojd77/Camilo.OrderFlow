using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.AssignPermissions;

public sealed record AssignPermissionsToRoleCommand(
    Guid RoleId,
    IReadOnlyCollection<Guid> PermissionIds) : IRequest<Unit>;
