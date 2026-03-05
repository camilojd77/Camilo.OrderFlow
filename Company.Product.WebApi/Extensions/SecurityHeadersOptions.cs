using System.ComponentModel.DataAnnotations;

namespace Company.Product.WebApi.Extensions;

/// <summary>
/// Controls the security response-header baseline.
/// Bound from "SecurityHeaders" in appsettings.json.
/// </summary>
public sealed class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>Master switch — set to false to skip all security headers (e.g. behind a gateway that sets them).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Emits Strict-Transport-Security only on HTTPS requests in non-Development environments.
    /// </summary>
    public bool HstsEnabled { get; init; } = true;

    /// <summary>DENY or SAMEORIGIN. DENY is safest; use SAMEORIGIN only if Swagger needs iframe embedding.</summary>
    [Required(ErrorMessage = "SecurityHeaders:XFrameOptions is required.")]
    public string XFrameOptions { get; init; } = "DENY";

    /// <summary>Controls the Referrer-Policy header value.</summary>
    [Required(ErrorMessage = "SecurityHeaders:ReferrerPolicy is required.")]
    public string ReferrerPolicy { get; init; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// When true, adds a Content-Security-Policy header.
    /// Disabled by default — enable only after verifying it does not break Swagger UI.
    /// </summary>
    public bool CspEnabled { get; init; } = false;

    /// <summary>Default policy is permissive enough for Swagger UI.</summary>
    public string ContentSecurityPolicy { get; init; } =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'";
}
