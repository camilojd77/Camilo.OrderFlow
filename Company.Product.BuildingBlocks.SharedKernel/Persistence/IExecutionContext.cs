namespace Company.Product.BuildingBlocks.SharedKernel.Persistence;

public interface IExecutionContext
{
    Task<int> CommitAsync(CancellationToken ct = default);
}
