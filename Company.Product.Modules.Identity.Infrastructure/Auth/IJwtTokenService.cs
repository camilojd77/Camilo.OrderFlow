using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresUtc) GenerateToken(User user);
}
