using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

/// <summary>
/// Deactivating a user ends their sessions at once: access tokens still within their lifetime are rejected and
/// refresh tokens are revoked.
/// </summary>
public sealed class SessionRevocationTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private const string Password = "Password1!";
    private readonly HttpClient _client = factory.CreateClient(new() { HandleCookies = false });

    [Fact]
    public async Task DeactivatedUser_AccessTokenStillValid_IsRejected()
    {
        var user = await RegisterAndLoginAsync();
        (await GetMeAsync(user.AccessToken)).ShouldBe(HttpStatusCode.OK);

        await DeactivateAsync(user.Id);

        (await GetMeAsync(user.AccessToken)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivatedUser_PermissionProtectedEndpoint_IsRejected()
    {
        var user = await RegisterAndLoginAsync();
        await DeactivateAsync(user.Id);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeactivatedUser_RefreshToken_IsRevoked()
    {
        var user = await RegisterAndLoginAsync();
        await DeactivateAsync(user.Id);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={user.RefreshToken}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReactivatedUser_CanLogInAgain()
    {
        var user = await RegisterAndLoginAsync();
        await DeactivateAsync(user.Id);
        await PostAsAdminAsync($"/api/users/{user.Id}/activate");

        var login = await LoginAsync(user.Email, Password);

        (await GetMeAsync(login.AccessToken)).ShouldBe(HttpStatusCode.OK);
    }

    private async Task<HttpStatusCode> GetMeAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private Task DeactivateAsync(Guid userId) => PostAsAdminAsync($"/api/users/{userId}/deactivate");

    private async Task PostAsAdminAsync(string path)
    {
        var admin = await LoginAsync("admin@company.com", "Admin123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<(Guid Id, string Email, string AccessToken, string RefreshToken)> RegisterAndLoginAsync()
    {
        var email = $"session-{Guid.NewGuid():N}@test.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = $"session-{Guid.NewGuid():N}"[..20], password = Password },
            TestContext.Current.CancellationToken);
        register.EnsureSuccessStatusCode();
        var id = (await ParseJsonAsync(register)).GetProperty("id").GetGuid();

        var login = await LoginAsync(email, Password);
        return (id, email, login.AccessToken, login.RefreshToken);
    }

    private async Task<(string AccessToken, string RefreshToken)> LoginAsync(string usernameOrEmail, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var accessToken = (await ParseJsonAsync(response)).GetProperty("accessToken").GetString()!;
        var refreshToken = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.Select(cookie => cookie.Split(';')[0])
                .Where(pair => pair.StartsWith("refresh_token=", StringComparison.Ordinal))
                .Select(pair => pair["refresh_token=".Length..])
                .FirstOrDefault() ?? string.Empty
            : string.Empty;

        return (accessToken, refreshToken);
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }
}
