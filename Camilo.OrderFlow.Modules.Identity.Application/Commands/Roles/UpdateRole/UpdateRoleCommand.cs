using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest<RoleDto>;
