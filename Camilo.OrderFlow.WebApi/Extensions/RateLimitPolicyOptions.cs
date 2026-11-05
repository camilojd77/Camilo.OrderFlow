using System.ComponentModel.DataAnnotations;

namespace Camilo.OrderFlow.WebApi.Extensions;

public sealed class RateLimitPolicyOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "PermitLimit must be at least 1.")]
    public int PermitLimit { get; set; }

    [Range(1, 86_400, ErrorMessage = "WindowSeconds must be between 1 and 86400 (24 h).")]
    public int WindowSeconds { get; set; } = 60;
}
