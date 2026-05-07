using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Domain.Entities;
using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Company.Product.Modules.Identity.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IExecutionContext _executionContext;
    private readonly LockoutOptions _lockoutOptions;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public AuthService(
        IdentityDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IExecutionContext executionContext,
        IOptions<LockoutOptions> lockoutOptions,
        IOptions<RefreshTokenOptions> refreshTokenOptions)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _executionContext = executionContext;
        _lockoutOptions = lockoutOptions.Value;
        _refreshTokenOptions = refreshTokenOptions.Value;
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

        if (user.IsLockedOut)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(
                _lockoutOptions.MaxAttempts,
                TimeSpan.FromMinutes(_lockoutOptions.DurationMinutes));
            await _executionContext.CommitAsync(cancellationToken);
            return null;
        }

        if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
            user.ResetFailedLoginAttempts();

        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var expiresAtUtc = DateTime.UtcNow.AddHours(_refreshTokenOptions.ExpiresHours);

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

        var newRawToken = GenerateRawToken();
        var newTokenHash = HashToken(newRawToken);
        var expiresAtUtc = DateTime.UtcNow.AddHours(_refreshTokenOptions.ExpiresHours);
        var newTokenId = Guid.NewGuid();

        var newRefreshToken = new RefreshToken(newTokenId, user.Id, newTokenHash, expiresAtUtc, clientIp);
        existing.Revoke(DateTime.UtcNow, newTokenId);

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

        existing.Revoke(DateTime.UtcNow);
        await _executionContext.CommitAsync(cancellationToken);
    }

    private static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
