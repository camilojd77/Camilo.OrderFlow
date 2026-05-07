using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Infrastructure.Auth;
using Company.Product.Modules.Identity.Infrastructure.Authorization;
using Company.Product.Modules.Identity.Infrastructure.Persistence;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Repositories;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
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

        services.AddOptions<LockoutOptions>().BindConfiguration(LockoutOptions.SectionName);
        services.AddOptions<RefreshTokenOptions>().BindConfiguration(RefreshTokenOptions.SectionName);

        // Permissions cache — L1 in-process (always) + L2 Redis (when Cache:Redis is set)
        var ttl = configuration.GetValue<int?>("Permissions:CacheTtlMinutes") ?? 10;
        var localTtl = configuration.GetValue<int?>("Permissions:LocalCacheTtlMinutes") ?? 5;
        services.AddHybridCache();
        services.AddSingleton(new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(ttl),
            LocalCacheExpiration = TimeSpan.FromMinutes(localTtl)
        });

        // Authorization
        services.AddAuthorization();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Token + auth services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Password hasher
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        // Execution context
        services.AddScoped<IExecutionContext, IdentityExecutionContext>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
