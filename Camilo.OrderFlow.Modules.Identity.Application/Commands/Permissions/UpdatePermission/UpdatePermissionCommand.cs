using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid Id, string Name) : IRequest<PermissionDto>;
