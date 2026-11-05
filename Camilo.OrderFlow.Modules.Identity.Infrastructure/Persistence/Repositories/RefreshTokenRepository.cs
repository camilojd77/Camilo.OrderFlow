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

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<bool> TryConsumeAsync(Guid tokenId, Guid replacedByTokenId, DateTime utcNow, CancellationToken ct = default)
    {
        var consumed = await _context.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.RevokedAtUtc, utcNow)
                .SetProperty(t => t.ReplacedByTokenId, replacedByTokenId), ct);

        return consumed == 1;
    }

    public Task<int> RevokeAllActiveAsync(Guid userId, DateTime utcNow, CancellationToken ct = default) =>
        _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAtUtc, utcNow), ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, DateTime utcNow, CancellationToken ct = default) =>
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ToListAsync(ct);

    public void Add(RefreshToken token) => _context.RefreshTokens.Add(token);
}
