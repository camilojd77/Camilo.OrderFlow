using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.DeletePermission;

public sealed record DeletePermissionCommand(Guid Id) : IRequest<Unit>;
