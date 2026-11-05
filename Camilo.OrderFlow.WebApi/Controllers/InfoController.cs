using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Camilo.OrderFlow.WebApi.Controllers;

/// <summary>
/// Operational info endpoint — no auth required.
/// Returns non-sensitive metadata about the running service.
/// </summary>
[ApiVersionNeutral]
[ApiController]
[Route("_info")]
public sealed class InfoController : ControllerBase
{
    private static readonly DateTime StartedAt = DateTime.UtcNow;

    private static readonly string ServiceName =
        Assembly.GetExecutingAssembly().GetName().Name ?? "unknown";

    private static readonly string ServiceVersion =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0";

    private readonly IHostEnvironment _env;

    public InfoController(IHostEnvironment env) => _env = env;

    /// <summary>
    /// GET /_info
    /// Returns non-sensitive service metadata useful for operations dashboards.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<InfoResponse>(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var response = new InfoResponse(
            ServiceName: ServiceName,
            ServiceVersion: ServiceVersion,
            Environment: _env.EnvironmentName,
            CommitSha: Environment.GetEnvironmentVariable("COMMIT_SHA") ?? string.Empty,
            BuildDate: Environment.GetEnvironmentVariable("BUILD_DATE") ?? string.Empty,
            UptimeSeconds: (long)(DateTime.UtcNow - StartedAt).TotalSeconds);

        return Ok(response);
    }
}

/// <param name="ServiceName">Assembly name of this service.</param>
/// <param name="ServiceVersion">Informational / semver version from the assembly.</param>
/// <param name="Environment">ASP.NET Core environment name (Development, Staging, Production).</param>
/// <param name="CommitSha">Git commit SHA injected at build/deploy time via COMMIT_SHA env var. Empty if not set.</param>
/// <param name="BuildDate">ISO-8601 build timestamp injected via BUILD_DATE env var. Empty if not set.</param>
/// <param name="UptimeSeconds">Seconds elapsed since the process started.</param>
public sealed record InfoResponse(
    string ServiceName,
    string ServiceVersion,
    string Environment,
    string CommitSha,
    string BuildDate,
    long UptimeSeconds);
