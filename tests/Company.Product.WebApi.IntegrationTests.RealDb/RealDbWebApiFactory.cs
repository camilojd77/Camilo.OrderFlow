using Company.Product.Modules.Identity.Infrastructure.DependencyInjection;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Company.Product.WebApi.IntegrationTests.RealDb;

/// <summary>
/// Spins up the full ASP.NET Core pipeline against a real SQL Server instance
/// (provided by Testcontainers via <see cref="SqlServerFixture"/>). Unlike the
/// fast SQLite-based factory, this one runs the actual EF Core migrations to
/// catch divergences between the model and the SQL Server schema.
/// </summary>
public sealed class RealDbWebApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public RealDbWebApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ConnectionStrings:Db"] = _connectionString,

                ["Jwt:Issuer"] = "RealDbTestIssuer",
                ["Jwt:Audience"] = "RealDbTestAudience",
                ["Jwt:Key"] = "RealDbTest_SuperSecretKey_AtLeast32Chars!",
                ["Jwt:ExpiresMinutes"] = "60",

                ["OpenTelemetry:Enabled"] = "false",

                ["RateLimiting:Default:PermitLimit"] = "10000",
                ["RateLimiting:Auth:PermitLimit"] = "10000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Drop the default IdentityDbContext options so we can re-register against the
            // Testcontainers SQL Server connection. EF Core 10 stores provider configuration
            // via IDbContextOptionsConfiguration<TContext>, so remove that descriptor too.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<IdentityDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.Name == "IDbContextOptionsConfiguration`1" &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(IdentityDbContext)))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<IdentityDbContext>(opt =>
                opt.UseSqlServer(_connectionString, sql =>
                    sql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Apply real migrations (no EnsureCreated — we want to validate the migration
        // history against the live engine) and seed identity data.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.Migrate();
        scope.ServiceProvider.SeedIdentityDataAsync().GetAwaiter().GetResult();

        return host;
    }
}
