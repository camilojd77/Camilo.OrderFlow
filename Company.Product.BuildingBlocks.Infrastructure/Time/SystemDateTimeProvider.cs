using Company.Product.BuildingBlocks.SharedKernel.Time;

namespace Company.Product.BuildingBlocks.Infrastructure.Time;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
