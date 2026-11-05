using Camilo.OrderFlow.BuildingBlocks.Infrastructure.Messaging;
using Camilo.OrderFlow.BuildingBlocks.Infrastructure.Time;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Camilo.OrderFlow.BuildingBlocks.Infrastructure.DependencyInjection;

public static class BuildingBlocksServiceCollectionExtensions
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }
}
