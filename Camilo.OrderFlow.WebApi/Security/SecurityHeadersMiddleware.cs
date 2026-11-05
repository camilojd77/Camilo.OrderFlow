using Camilo.OrderFlow.WebApi.Extensions;
using Microsoft.Extensions.Options;

namespace Camilo.OrderFlow.WebApi.Security;

/// <summary>
/// Adds a security response-header baseline to every response.
/// Configured via <see cref="SecurityHeadersOptions"/> ("SecurityHeaders" section).
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly IHostEnvironment _env;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        IHostEnvironment env)
    {
        _next = next;
        _options = options.Value;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Enabled)
        {
            var headers = context.Response.Headers;

            // HSTS — only on HTTPS and never in Development (avoids accidental local pinning).
            if (_options.HstsEnabled && context.Request.IsHttps && !_env.IsDevelopment())
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // Prevent MIME-type sniffing.
            headers["X-Content-Type-Options"] = "nosniff";

            // Clickjacking protection — DENY or SAMEORIGIN (configurable).
            headers["X-Frame-Options"] = _options.XFrameOptions;

            // Reduce referrer leakage to third-party origins.
            headers["Referrer-Policy"] = _options.ReferrerPolicy;

            // Disable browser features not needed by an API.
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // CSP — disabled by default; enable only after verifying Swagger is not broken.
            if (_options.CspEnabled && !string.IsNullOrWhiteSpace(_options.ContentSecurityPolicy))
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        await _next(context);
    }
}
