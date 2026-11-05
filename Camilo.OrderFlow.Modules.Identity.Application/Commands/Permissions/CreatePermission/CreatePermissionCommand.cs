using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.CreatePermission;

public sealed record CreatePermissionCommand(string Code, string Name) : IRequest<PermissionDto>;
