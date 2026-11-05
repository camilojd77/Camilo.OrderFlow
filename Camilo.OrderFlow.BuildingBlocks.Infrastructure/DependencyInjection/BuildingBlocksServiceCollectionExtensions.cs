using Camilo.OrderFlow.BuildingBlocks.Infrastructure.Messaging;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Camilo.OrderFlow.BuildingBlocks.Infrastructure.DependencyInjection;

public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
