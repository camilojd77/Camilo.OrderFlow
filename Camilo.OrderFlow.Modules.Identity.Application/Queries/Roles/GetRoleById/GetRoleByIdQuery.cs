using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Roles.GetRoleById;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;
