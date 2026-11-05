using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

internal interface IJwtTokenService
{
    (string Token, DateTime ExpiresUtc) GenerateToken(User user);
}
