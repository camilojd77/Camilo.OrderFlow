using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl) : IRequest<UserDto>;
