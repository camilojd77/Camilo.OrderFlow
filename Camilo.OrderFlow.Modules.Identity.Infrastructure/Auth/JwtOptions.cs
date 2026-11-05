using System.ComponentModel.DataAnnotations;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Issuer is required.")]
    public string Issuer { get; init; } = default!;

    [Required(ErrorMessage = "Jwt:Audience is required.")]
    public string Audience { get; init; } = default!;

    [Required(ErrorMessage = "Jwt:Key is required.")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters.")]
    public string Key { get; init; } = default!;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpiresMinutes must be between 1 and 1440.")]
    public int ExpiresMinutes { get; init; } = 60;
}
