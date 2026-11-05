using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly LockoutOptions _lockoutOptions;
    private readonly RefreshTokenOptions _refreshTokenOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IdentityDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityExecutionContext executionContext,
        IOptions<LockoutOptions> lockoutOptions,
        IOptions<RefreshTokenOptions> refreshTokenOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _executionContext = executionContext;
        _lockoutOptions = lockoutOptions.Value;
        _refreshTokenOptions = refreshTokenOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var normalizedInput = request.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Where(u => u.IsActive &&
                        (u.Email == normalizedInput || u.UserName.ToLower() == normalizedInput))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (user.IsLockedOut(utcNow))
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(
                _lockoutOptions.MaxAttempts,
                TimeSpan.FromMinutes(_lockoutOptions.DurationMinutes),
                utcNow);
            await _executionContext.CommitAsync(cancellationToken);
            return null;
        }

        if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
            user.ResetFailedLoginAttempts(utcNow);

        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var expiresAtUtc = utcNow.AddHours(_refreshTokenOptions.ExpiresHours);

        var refreshToken = new RefreshToken(Guid.NewGuid(), user.Id, tokenHash, expiresAtUtc, clientIp);
        _refreshTokenRepository.Add(refreshToken);

        await _executionContext.CommitAsync(cancellationToken);

        var (accessToken, accessExpiresUtc) = _jwtTokenService.GenerateToken(user);

        return new LoginResponse(
            accessToken,
            accessExpiresUtc,
            new LoginUserDto(user.Id, user.UserName, user.Email),
            rawToken);
    }

    public async Task<RefreshResponse?> RefreshAsync(
        string rawRefreshToken,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var existing = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
            return null;

        var user = await _context.Users
            .Where(u => u.Id == existing.UserId && u.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var newRawToken = GenerateRawToken();
        var newTokenHash = HashToken(newRawToken);
        var expiresAtUtc = utcNow.AddHours(_refreshTokenOptions.ExpiresHours);
        var newTokenId = Guid.NewGuid();

        var newRefreshToken = new RefreshToken(newTokenId, user.Id, newTokenHash, expiresAtUtc, clientIp);
        existing.Revoke(utcNow, newTokenId);

        _refreshTokenRepository.Add(newRefreshToken);
        await _executionContext.CommitAsync(cancellationToken);

        var (accessToken, accessExpiresUtc) = _jwtTokenService.GenerateToken(user);

        return new RefreshResponse(accessToken, accessExpiresUtc, newRawToken);
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var existing = await _refreshTokenRepository.GetActiveByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
            return;

        existing.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
        await _executionContext.CommitAsync(cancellationToken);
    }

    private static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
