using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Time;

namespace Camilo.OrderFlow.BuildingBlocks.Infrastructure.Time;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
