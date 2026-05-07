using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Queries.Roles.ListRoles;

public sealed record ListRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
