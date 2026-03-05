namespace Company.Product.WebApi.Security;

/// <summary>
/// CORS policy configuration. Bound from "Cors" in appsettings.json.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Master switch — set to false to disable CORS (e.g. purely internal API).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Origins allowed to call the API.
    /// Must not be empty when <see cref="Enabled"/> is true — validated at startup.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Allows cookies and Authorization headers on cross-origin requests.
    /// Requires <see cref="AllowedOrigins"/> to contain explicit origins (no wildcard).
    /// </summary>
    public bool AllowCredentials { get; init; } = false;
}
