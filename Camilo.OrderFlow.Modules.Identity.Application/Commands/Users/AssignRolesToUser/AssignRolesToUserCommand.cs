using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.AssignRolesToUser;

public sealed record AssignRolesToUserCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> RoleIds) : IRequest<Unit>;
