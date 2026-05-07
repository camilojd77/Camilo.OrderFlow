using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    string? AvatarUrl = null) : IRequest<UserDto>;
