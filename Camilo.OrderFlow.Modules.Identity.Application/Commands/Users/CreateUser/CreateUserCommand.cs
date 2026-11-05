using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string UserName,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    string? AvatarUrl = null) : IRequest<UserDto>;
