using System.ComponentModel.DataAnnotations;

namespace Camilo.OrderFlow.WebApi.Observability.OpenTelemetry;

public sealed class OpenTelemetryOptions
{
    internal const string SectionName = "OpenTelemetry";

    public bool Enabled { get; init; } = true;

    [Required(ErrorMessage = "OpenTelemetry:ServiceName is required.")]
    public string ServiceName { get; init; } = "Camilo.OrderFlow.WebApi";

    [Required(ErrorMessage = "OpenTelemetry:ServiceVersion is required.")]
    public string ServiceVersion { get; init; } = "1.0.0";

    [Required(ErrorMessage = "OpenTelemetry:Environment is required.")]
    public string Environment { get; init; } = "dev";

    public string? OtlpEndpoint { get; init; } = "http://localhost:4317";

    [Required(ErrorMessage = "OpenTelemetry:ExporterProtocol is required.")]
    public string ExporterProtocol { get; init; } = "grpc";

    [Range(0.0, 1.0, ErrorMessage = "OpenTelemetry:SampleRatio must be between 0.0 and 1.0.")]
    public double SampleRatio { get; init; } = 1.0;

    public bool EnableSqlClientInstrumentation { get; init; } = false;

    /// <summary>
    /// When true, the raw SQL text is captured in db.statement spans.
    /// Keep false in production to avoid leaking sensitive query data.
    /// </summary>
    public bool DbTracingCaptureStatements { get; init; } = false;

    public bool EnableConsoleExporter { get; init; } = false;
}
