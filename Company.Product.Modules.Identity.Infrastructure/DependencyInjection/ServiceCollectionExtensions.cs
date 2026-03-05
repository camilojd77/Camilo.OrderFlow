using Company.Product.Modules.Identity.Infrastructure.Auth;
using Company.Product.Modules.Identity.Infrastructure.Authorization;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Product.Modules.Identity.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");

        services.AddDbContext<IdentityDbContext>(opt =>
        {
            opt.UseSqlServer(cs, sql =>
            {
                sql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
            });
        });

        services.AddScoped<IdentityDataSeeder>();

        // JWT options — consumed by JwtTokenService and WebApi auth registration.
        // ValidateOnStart rejects app startup when Jwt:Key is missing or < 32 chars.
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Authorization
        services.AddAuthorization();
        services.AddMemoryCache();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Token + auth services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Aquí después registramos repositorios (IUserRepository, etc.)
        return services;
    }
}
