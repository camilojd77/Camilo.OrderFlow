using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(RefreshToken token);
}
