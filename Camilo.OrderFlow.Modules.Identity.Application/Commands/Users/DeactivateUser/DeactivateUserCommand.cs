using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<Unit>;
