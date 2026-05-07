using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Domain.Entities;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Company.Product.Modules.Identity.Infrastructure.Persistence.Repositories;

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
