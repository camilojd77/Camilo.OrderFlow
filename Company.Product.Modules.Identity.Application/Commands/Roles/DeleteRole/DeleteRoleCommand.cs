using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.DeleteRole;

public sealed record DeleteRoleCommand(Guid Id) : IRequest<Unit>;
