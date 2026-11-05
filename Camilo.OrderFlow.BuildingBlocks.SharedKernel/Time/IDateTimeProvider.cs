namespace Camilo.OrderFlow.BuildingBlocks.SharedKernel.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
