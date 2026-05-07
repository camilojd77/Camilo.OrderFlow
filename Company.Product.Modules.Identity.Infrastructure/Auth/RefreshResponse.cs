namespace Company.Product.Modules.Identity.Infrastructure.Auth;

public sealed record RefreshResponse(
    string AccessToken,
    DateTime ExpiresUtc,
    string RawRefreshToken);
