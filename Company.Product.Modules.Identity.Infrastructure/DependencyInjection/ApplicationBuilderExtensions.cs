using Company.Product.Modules.Identity.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Product.Modules.Identity.Infrastructure.DependencyInjection;

public static class ApplicationBuilderExtensions
{
    public static async Task SeedIdentityDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
