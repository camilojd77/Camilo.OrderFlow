namespace Company.Product.WebApi.Extensions;

internal static class SecurityHeadersServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SecurityHeadersOptions"/> with fail-fast DataAnnotations validation.
    /// Pair with <c>app.UseSecurityHeaders()</c> in the pipeline.
    /// </summary>
    internal static IServiceCollection AddAppSecurityHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SecurityHeadersOptions>()
            .BindConfiguration(SecurityHeadersOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
