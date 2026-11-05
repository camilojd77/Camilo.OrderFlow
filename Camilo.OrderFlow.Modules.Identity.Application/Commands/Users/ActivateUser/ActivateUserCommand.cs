using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : IRequest<Unit>;
