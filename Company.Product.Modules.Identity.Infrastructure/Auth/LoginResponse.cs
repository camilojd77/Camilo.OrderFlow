namespace Company.Product.Modules.Identity.Infrastructure.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresUtc,
    LoginUserDto User);

public sealed record LoginUserDto(Guid Id, string UserName, string Email);
