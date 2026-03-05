using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Product.WebApi.IntegrationTests;

/// <summary>
/// Spins up the full ASP.NET Core pipeline in-process with:
///  • InMemory EF Core database (no SQL Server required)
///  • Overridden appsettings suitable for test isolation
///  • OTel disabled to avoid exporter noise in test output
/// </summary>
public sealed class WebApiFactory : WebApplicationFactory<Program>
{
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
            // Replace the SQL Server DbContext with an in-memory provider so the
            // data seeder and EF Core queries work without a real database.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<IdentityDbContext>(opt =>
                opt.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }
}
