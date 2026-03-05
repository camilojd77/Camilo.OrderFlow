using Company.Product.WebApi.Security;

namespace Company.Product.WebApi.Extensions;

internal static class CorsServiceCollectionExtensions
{
    /// <summary>
    /// Registers a "default" CORS policy from the "Cors" configuration section.
    /// Fails at startup when <c>Cors:Enabled</c> is true but <c>AllowedOrigins</c> is empty.
    /// When <c>Enabled</c> is false no CORS policy is registered and <c>UseCors</c> is skipped.
    /// </summary>
    internal static IServiceCollection AddAppCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                opts => !opts.Enabled || opts.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins must not be empty when Cors:Enabled is true.")
            .ValidateOnStart();

        var opts = configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>() ?? new();

        if (!opts.Enabled)
            return services;

        services.AddCors(cors =>
        {
            cors.AddPolicy("default", policy =>
            {
                policy.WithOrigins(opts.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();

                if (opts.AllowCredentials)
                    policy.AllowCredentials();
                else
                    policy.DisallowCredentials();
            });
        });

        return services;
    }
}
