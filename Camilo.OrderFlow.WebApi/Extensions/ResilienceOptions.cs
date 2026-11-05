using System.ComponentModel.DataAnnotations;

namespace Camilo.OrderFlow.WebApi.Extensions;

/// <summary>
/// Options for a resilient HTTP client. Bound from "Resilience:{ClientName}" in appsettings.json.
/// To add a new client, copy the "ErpClient" section and register it in AddResilientHttpClients.
/// </summary>
public sealed class HttpClientResilienceOptions
{
    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300.")]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10, ErrorMessage = "RetryCount must be between 0 and 10.")]
    public int RetryCount { get; set; } = 3;

    [Range(0, 60_000, ErrorMessage = "BaseDelayMs must be between 0 and 60000.")]
    public int BaseDelayMs { get; set; } = 200;

    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}
