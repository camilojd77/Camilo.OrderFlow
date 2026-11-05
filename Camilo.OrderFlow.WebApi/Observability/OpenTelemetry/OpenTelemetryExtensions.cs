using Camilo.OrderFlow.WebApi.Observability.Processors;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Camilo.OrderFlow.WebApi.Observability.OpenTelemetry;

internal static class OpenTelemetryExtensions
{
    internal static IServiceCollection AddOpenTelemetryObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Always bind options so they can be injected elsewhere (e.g. diagnostics endpoints).
        // ValidateOnStart ensures the app refuses to start when the section is misconfigured.
        services
            .AddOptions<OpenTelemetryOptions>()
            .BindConfiguration(OpenTelemetryOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (!options.Enabled)
            return services;

        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            throw new InvalidOperationException(
                "OpenTelemetry:OtlpEndpoint must be configured when OpenTelemetry is enabled.");

        var otlpEndpoint = new Uri(options.OtlpEndpoint);
        var otlpProtocol = options.ExporterProtocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = options.Environment,
            });

        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SampleRatio)))
                    .AddAspNetCoreInstrumentation(aspnet =>
                    {
                        // Enrich each span with the business correlation ID once the
                        // CorrelationIdMiddleware has populated HttpContext.Items.
                        aspnet.EnrichWithHttpRequest = (activity, response) =>
                        {
                            if (response.HttpContext.Items.TryGetValue(Correlation.ItemKey, out var correlationId)
                                && correlationId is string cid
                                && !string.IsNullOrEmpty(cid))
                            {
                                activity.SetTag("correlation_id", cid);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = otlpEndpoint;
                        otlp.Protocol = otlpProtocol;
                    });

                if (options.EnableSqlClientInstrumentation)
                {
                    tracing.AddSqlClientInstrumentation(sql =>
                    {
                        sql.RecordException = true;
                    });

                    // Post-process completed spans: strip raw SQL text when capture is disabled
                    // so production traces never expose sensitive query data.
                    tracing.AddProcessor(new DbStatementRedactionProcessor(options.DbTracingCaptureStatements));
                }

                if (options.EnableConsoleExporter)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = otlpEndpoint;
                        otlp.Protocol = otlpProtocol;
                    });

                if (options.EnableConsoleExporter)
                    metrics.AddConsoleExporter();
            });

        return services;
    }
}
