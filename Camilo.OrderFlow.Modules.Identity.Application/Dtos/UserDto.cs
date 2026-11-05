namespace Camilo.OrderFlow.Modules.Identity.Application.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string UserName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    UserProfileDto? Profile);
