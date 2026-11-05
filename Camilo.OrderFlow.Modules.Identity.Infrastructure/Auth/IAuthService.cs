namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken = default);
    Task<RefreshResponse?> RefreshAsync(string rawRefreshToken, string clientIp, CancellationToken cancellationToken = default);
    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the user exists and is active. Called for every authenticated request so that a deactivated user's
    /// access tokens stop working immediately instead of when they expire.
    /// </summary>
    Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
