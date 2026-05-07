using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<RoleDto>;
