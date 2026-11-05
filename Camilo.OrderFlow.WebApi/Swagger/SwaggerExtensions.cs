using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;

namespace Camilo.OrderFlow.WebApi.Swagger;

internal static class SwaggerExtensions
{
    /// <summary>
    /// Registers Swagger with JWT bearer security and per-version document generation.
    /// </summary>
    internal static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            // JWT bearer — shows the padlock icon in Swagger UI
            options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Escribe: Bearer {tu_token}",
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = [],
            });
        });

        // Adds one Swagger doc per API version (resolved via IApiVersionDescriptionProvider)
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        return services;
    }

    /// <summary>
    /// Enables Swagger UI with endpoints for each discovered API version.
    /// </summary>
    internal static WebApplication UseAppSwagger(this WebApplication app)
    {
        app.UseSwagger();

        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwaggerUI(ui =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                ui.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Camilo.OrderFlow API {description.GroupName}");
            }
        });

        return app;
    }
}
