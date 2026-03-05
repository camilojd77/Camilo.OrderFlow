using Company.Product.Modules.Identity.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace Company.Product.Modules.Identity.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IdentityDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedInput = request.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Where(u => u.IsActive &&
                        (u.Email == normalizedInput ||
                         u.UserName.ToLower() == normalizedInput))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !BC.Verify(request.Password, user.PasswordHash))
            return null;

        var (token, expiresUtc) = _jwtTokenService.GenerateToken(user);

        return new LoginResponse(
            token,
            expiresUtc,
            new LoginUserDto(user.Id, user.UserName, user.Email));
    }
}
