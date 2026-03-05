using System.ComponentModel.DataAnnotations;

namespace Company.Product.WebApi.Extensions;

public sealed class CircuitBreakerOptions
{
    [Range(0.01, 1.0, ErrorMessage = "FailureThreshold must be between 0.01 and 1.0.")]
    public double FailureThreshold { get; set; } = 0.5;

    [Range(1, 3600, ErrorMessage = "SamplingDurationSeconds must be between 1 and 3600.")]
    public int SamplingDurationSeconds { get; set; } = 30;

    [Range(1, 10_000, ErrorMessage = "MinimumThroughput must be between 1 and 10000.")]
    public int MinimumThroughput { get; set; } = 20;

    [Range(1, 3600, ErrorMessage = "BreakDurationSeconds must be between 1 and 3600.")]
    public int BreakDurationSeconds { get; set; } = 20;
}
