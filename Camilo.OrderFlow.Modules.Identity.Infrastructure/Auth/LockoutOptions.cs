namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public sealed class LockoutOptions
{
    public const string SectionName = "Lockout";
    public int MaxAttempts { get; init; } = 5;
    public int DurationMinutes { get; init; } = 15;
}
