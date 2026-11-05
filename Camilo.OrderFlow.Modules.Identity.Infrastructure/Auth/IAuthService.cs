namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken = default);
    Task<RefreshResponse?> RefreshAsync(string rawRefreshToken, string clientIp, CancellationToken cancellationToken = default);
    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
}
