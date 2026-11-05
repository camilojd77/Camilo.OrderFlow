using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresUtc) GenerateToken(User user);
}
