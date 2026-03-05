namespace Company.Product.WebApi.Extensions;

/// <summary>
/// Options for the rate limiting middleware. Bound from "RateLimiting" in appsettings.json.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicyOptions Default { get; set; } = new() { PermitLimit = 100, WindowSeconds = 60 };
    public RateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}
