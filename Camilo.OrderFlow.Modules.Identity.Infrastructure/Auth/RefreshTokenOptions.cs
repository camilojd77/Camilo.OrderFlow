namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

internal sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";
    public int ExpiresHours { get; init; } = 720; // 30 days
}
