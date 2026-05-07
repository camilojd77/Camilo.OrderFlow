using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? Description) : IRequest<RoleDto>;
