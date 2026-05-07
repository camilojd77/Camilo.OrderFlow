using Company.Product.BuildingBlocks.Infrastructure.Messaging;
using Company.Product.BuildingBlocks.Infrastructure.Time;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Product.BuildingBlocks.Infrastructure.DependencyInjection;

public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }
}
