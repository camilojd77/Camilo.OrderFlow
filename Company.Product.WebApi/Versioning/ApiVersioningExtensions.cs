using Asp.Versioning;

namespace Company.Product.WebApi.Versioning;

internal static class ApiVersioningExtensions
{
    /// <summary>
    /// Configures API versioning by header ("api-version").
    /// Requests without the header default to v1.0 — existing clients keep working.
    /// </summary>
    internal static IServiceCollection AddAppApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new HeaderApiVersionReader("api-version");
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";   // v1, v1.1, v2, etc.
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
