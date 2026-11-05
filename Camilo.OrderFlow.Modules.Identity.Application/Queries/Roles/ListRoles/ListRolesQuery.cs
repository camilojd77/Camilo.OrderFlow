using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Roles.ListRoles;

public sealed record ListRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
