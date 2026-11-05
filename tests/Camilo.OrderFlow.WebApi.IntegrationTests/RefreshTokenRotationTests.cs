using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

/// <summary>
/// docs/adr/0029-revocacion-de-sesiones.md: a refresh token is single-use, and presenting one that was already
/// rotated revokes every active refresh token of the user (possible theft).
/// </summary>
public sealed class RefreshTokenRotationTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private const string Password = "Password1!";
    private readonly HttpClient _client = factory.CreateClient(new() { HandleCookies = false });

    [Fact]
    public async Task Refresh_ValidToken_RotatesIt()
    {
        var (first, _) = await RegisterAndLoginAsync();

        var (status, second) = await RefreshAsync(first);

        status.ShouldBe(HttpStatusCode.OK);
        second.ShouldNotBeNullOrEmpty();
        second.ShouldNotBe(first);
    }

    [Fact]
    public async Task Refresh_RotatedTokenReused_IsRejectedAndRevokesTheNewOne()
    {
        var (first, _) = await RegisterAndLoginAsync();
        var (_, second) = await RefreshAsync(first);

        var (reuseStatus, _) = await RefreshAsync(first);
        var (afterReuseStatus, _) = await RefreshAsync(second!);

        reuseStatus.ShouldBe(HttpStatusCode.Unauthorized);
        afterReuseStatus.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_LoggedOutToken_IsRejected()
    {
        var (token, accessToken) = await RegisterAndLoginAsync();
        using (var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout"))
        {
            logout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            logout.Headers.Add("Cookie", $"refresh_token={token}");
            (await _client.SendAsync(logout, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        }

        var (status, _) = await RefreshAsync(token);

        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<(HttpStatusCode Status, string? RefreshToken)> RefreshAsync(string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        return (response.StatusCode, ExtractRefreshToken(response));
    }

    private async Task<(string RefreshToken, string AccessToken)> RegisterAndLoginAsync()
    {
        var email = $"rotation-{Guid.NewGuid():N}@test.com";
        (await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = $"rot-{Guid.NewGuid():N}"[..20], password = Password },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = Password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var accessToken = JsonDocument.Parse(json).RootElement.GetProperty("accessToken").GetString()!;
        var refreshToken = ExtractRefreshToken(login) ?? throw new InvalidOperationException("Login did not set the refresh cookie.");
        return (refreshToken, accessToken);
    }

    private static string? ExtractRefreshToken(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.Select(cookie => cookie.Split(';')[0])
                .Where(pair => pair.StartsWith("refresh_token=", StringComparison.Ordinal))
                .Select(pair => pair["refresh_token=".Length..])
                .FirstOrDefault(value => value.Length > 0)
            : null;
}
