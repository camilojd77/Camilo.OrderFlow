namespace Company.Product.BuildingBlocks.SharedKernel.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
