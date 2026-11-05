using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>The token with this hash whatever its state (active, rotated, revoked or expired); not tracked.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Atomically marks the token as rotated only if it is still active. Returns false when another request consumed
    /// it first. Runs immediately (not deferred to the commit); call it inside the rotation transaction.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid tokenId, Guid replacedByTokenId, DateTime utcNow, CancellationToken ct = default);

    /// <summary>Revokes every active refresh token of the user at once (reuse of a rotated token). Runs immediately.</summary>
    Task<int> RevokeAllActiveAsync(Guid userId, DateTime utcNow, CancellationToken ct = default);
    /// <summary>Tracked refresh tokens of the user that are neither revoked nor expired at <paramref name="utcNow"/>.</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, DateTime utcNow, CancellationToken ct = default);

    void Add(RefreshToken token);
}
