using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context) => _context = context;

    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.RefreshTokens
            .Where(t => t.TokenHash == tokenHash && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

    public void Add(RefreshToken token) => _context.RefreshTokens.Add(token);
}
