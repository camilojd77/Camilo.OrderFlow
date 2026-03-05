using Company.Product.WebApi.Security;

namespace Company.Product.WebApi.Extensions;

internal static class SecurityHeadersApplicationBuilderExtensions
{
    /// <summary>
    /// Inserts the security-headers middleware into the pipeline.
    /// Configure via "SecurityHeaders" in appsettings.
    /// Requires <c>services.AddAppSecurityHeaders()</c> to be called first.
    /// </summary>
    internal static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
