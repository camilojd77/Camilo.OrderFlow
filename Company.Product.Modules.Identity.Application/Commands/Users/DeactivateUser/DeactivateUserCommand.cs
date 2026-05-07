using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<Unit>;
