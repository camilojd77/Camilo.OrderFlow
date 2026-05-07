using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Company.Product.WebApi.Observability.Health;

internal static class HealthChecksExtensions
{
    public static IServiceCollection AddAppHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate the connection string exists at startup (fail-fast).
        _ = configuration.GetConnectionString("Db")
            ?? throw new InvalidOperationException("Missing connection string: Db");

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            // Use a factory so the connection string is resolved from IConfiguration at request
            // time, not at registration time. This ensures test overrides (via ConfigureAppConfiguration)
            // are respected and avoids capturing a stale empty string from appsettings.json.
            .AddSqlServer(
                connectionStringFactory: sp =>
                    sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")
                    ?? throw new InvalidOperationException("Missing connection string: Db"),
                name: "sqlserver",
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness: no external dependency checks — always 200 if the process responds
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
        });

        // Readiness: only checks tagged "ready" (e.g. SQL Server)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Aggregate: all registered checks
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        return endpoints;
    }
}
