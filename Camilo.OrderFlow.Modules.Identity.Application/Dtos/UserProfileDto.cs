namespace Camilo.OrderFlow.Modules.Identity.Application.Dtos;

public sealed record UserProfileDto(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl);
