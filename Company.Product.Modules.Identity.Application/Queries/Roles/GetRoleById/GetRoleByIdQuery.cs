using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Queries.Roles.GetRoleById;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;
