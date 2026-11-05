using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityExecutionContext : IIdentityExecutionContext
{
    private readonly IdentityDbContext _dbContext;

    public IdentityExecutionContext(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
