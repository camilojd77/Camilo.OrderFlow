using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string UserName, string Password) : IRequest<UserDto>;
