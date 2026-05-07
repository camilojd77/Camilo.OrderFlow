using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityExecutionContext : IExecutionContext
{
    private readonly IdentityDbContext _dbContext;

    public IdentityExecutionContext(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
