using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Users.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : IRequest<Unit>;
