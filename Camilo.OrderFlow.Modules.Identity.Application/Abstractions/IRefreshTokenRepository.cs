using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(RefreshToken token);
}
