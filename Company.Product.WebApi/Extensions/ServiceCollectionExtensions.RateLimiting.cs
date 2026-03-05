using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

namespace Company.Product.WebApi.Extensions;

internal static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Configures the ASP.NET Core rate limiting middleware with two policies:
    ///   "default" — moderate per-IP limit applied to all controller endpoints.
    ///   "auth"    — stricter per-IP limit for authentication endpoints.
    /// Apply [EnableRateLimiting("auth")] on controllers that need the stricter policy.
    /// </summary>
    internal static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Fail-fast: validate options at startup (including nested PermitLimit/WindowSeconds ranges)
        services
            .AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                opts => opts.Default.PermitLimit >= 1 && opts.Default.WindowSeconds >= 1
                     && opts.Auth.PermitLimit >= 1 && opts.Auth.WindowSeconds >= 1,
                "RateLimiting: PermitLimit and WindowSeconds must be >= 1 for both Default and Auth policies.")
            .ValidateOnStart();

        var options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Return a ProblemDetails response consistent with the global exception handler
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                // Use the registered IProblemDetailsService so traceId/correlationId extensions apply
                var problemService = context.HttpContext.RequestServices
                    .GetService<IProblemDetailsService>();

                if (problemService is not null)
                {
                    await problemService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too Many Requests",
                            Type = "https://httpstatuses.com/429",
                            Detail = "Rate limit exceeded. Try again later.",
                            Instance = context.HttpContext.Request.Path,
                        },
                    });
                }
            };

            // "default" policy — per IP, fixed window
            limiter.AddPolicy("default", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.Default.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.Default.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // "auth" policy — per IP, fixed window, stricter limit
            limiter.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.Auth.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
