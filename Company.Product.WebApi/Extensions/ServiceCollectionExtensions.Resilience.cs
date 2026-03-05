using Company.Product.WebApi.Infrastructure.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Company.Product.WebApi.Extensions;

internal static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Registers resilient named HttpClients with timeout, retry and circuit-breaker policies.
    /// Each client propagates X-Correlation-Id and is fully observable via OTel (HttpClient instrumentation).
    /// To add another client (e.g. "PaymentsClient"), copy the AddResilientClient line
    /// and add its configuration section in appsettings.json under Resilience:PaymentsClient.
    /// </summary>
    internal static IServiceCollection AddResilientHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddResilientClient("ErpClient", configuration);
        // services.AddResilientClient("PaymentsClient", configuration);

        return services;
    }

    private static void AddResilientClient(
        this IServiceCollection services,
        string clientName,
        IConfiguration configuration)
    {
        // Fail-fast: validate options at startup before any request is served
        services
            .AddOptions<HttpClientResilienceOptions>(clientName)
            .BindConfiguration($"Resilience:{clientName}")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var section = configuration.GetSection($"Resilience:{clientName}");
        var options = section.Get<HttpClientResilienceOptions>() ?? new();

        services.AddHttpClient(clientName)
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddResilienceHandler($"{clientName}-pipeline", builder =>
            {
                // Strategies execute outermost → innermost: Retry → CircuitBreaker → Timeout

                // 1. Retry on transient errors (5xx, 408, HttpRequestException, TimeoutRejectedException)
                //    with exponential backoff + jitter
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryCount,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(options.BaseDelayMs),
                });

                // 2. Circuit breaker — opens when failure ratio exceeds threshold
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreaker.FailureThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                    MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                });

                // 3. Timeout per individual attempt
                builder.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
            });
    }
}
