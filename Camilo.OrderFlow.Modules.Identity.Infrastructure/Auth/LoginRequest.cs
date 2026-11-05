namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

public sealed record LoginRequest(string UsernameOrEmail, string Password);
