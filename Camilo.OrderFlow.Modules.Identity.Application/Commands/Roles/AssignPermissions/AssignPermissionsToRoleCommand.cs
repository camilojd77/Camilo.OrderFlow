using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.AssignPermissions;

public sealed record AssignPermissionsToRoleCommand(
    Guid RoleId,
    IReadOnlyCollection<Guid> PermissionIds) : IRequest<Unit>;
