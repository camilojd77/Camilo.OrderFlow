using Company.Product.Modules.Identity.Infrastructure.DependencyInjection;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Company.Product.WebApi.IntegrationTests;

/// <summary>
/// Spins up the full ASP.NET Core pipeline in-process with:
///  • SQLite in-memory database (fast, no SQL Server required, respects FK constraints)
///  • Overridden appsettings suitable for test isolation
///  • OTel disabled to avoid exporter noise in test output
/// </summary>
public sealed class WebApiFactory : WebApplicationFactory<Program>
{
    // Keep the connection alive for the lifetime of the factory so the in-memory DB persists.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public WebApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Connection strings must be present (HealthChecks.AddSqlServer reads them),
                // but won't be exercised for /health/live tests.
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=IntegrationTestDb;Trusted_Connection=true",
                ["ConnectionStrings:Db"]               = "Server=(localdb)\\mssqllocaldb;Database=IntegrationTestDb;Trusted_Connection=true",

                // Minimal valid JWT section so JwtOptions passes ValidateOnStart
                ["Jwt:Issuer"]         = "IntegrationTestIssuer",
                ["Jwt:Audience"]       = "IntegrationTestAudience",
                ["Jwt:Key"]            = "IntegrationTest_SuperSecretKey_AtLeast32Chars!",
                ["Jwt:ExpiresMinutes"] = "60",

                // Disable OTel exporter to avoid connection errors to a non-existent collector
                ["OpenTelemetry:Enabled"] = "false",

                // Relax rate limits so tests don't throttle themselves
                ["RateLimiting:Default:PermitLimit"] = "10000",
                ["RateLimiting:Auth:PermitLimit"]    = "10000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 10 uses IDbContextOptionsConfiguration<TContext> to store the provider
            // configuration (instead of registering IDatabaseProvider directly). Remove all
            // options-related descriptors for IdentityDbContext before registering SQLite.
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
                opt.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create schema then seed — must happen after the host is built but before tests run.
        // The app startup does NOT seed (Seed:Enabled is not set), so we do it here in the
        // correct order: schema first, data second.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.EnsureCreated();
        scope.ServiceProvider.SeedIdentityDataAsync().GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
