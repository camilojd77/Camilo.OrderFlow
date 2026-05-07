using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Users.AssignRolesToUser;

public sealed record AssignRolesToUserCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> RoleIds) : IRequest<Unit>;
