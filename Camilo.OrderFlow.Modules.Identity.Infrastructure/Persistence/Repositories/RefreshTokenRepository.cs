using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenRepository(IdentityDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        return _context.RefreshTokens
            .Where(t => t.TokenHash == tokenHash && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .FirstOrDefaultAsync(ct);
    }

    public void Add(RefreshToken token) => _context.RefreshTokens.Add(token);
}
