using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<RoleDto>;
