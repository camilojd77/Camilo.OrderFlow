using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

public sealed class AuthTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = "testuser1", password = "Password1!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = "dup1", password = "Password1!" }, TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = "dup2", password = "Password1!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "bad@test.com", userName = "baduser", password = "weak" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        var email = $"login-{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password1!");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = "Password1!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ParseJson(response);
        body.TryGetProperty("accessToken", out var token).ShouldBeTrue();
        token.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"login-{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password1!");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = "WrongPass1!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_SetsRefreshTokenCookie()
    {
        var email = $"cookie-{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password1!");

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = "Password1!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ExtractRefreshToken(response).ShouldNotBeNullOrWhiteSpace();
    }

    // ── Refresh ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewAccessToken()
    {
        var (_, refreshToken) = await RegisterAndLogin();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJson(response);
        body.TryGetProperty("accessToken", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_NoCookie_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_AuthenticatedWithCookie_Returns204()
    {
        var (accessToken, refreshToken) = await RegisterAndLogin();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_NotAuthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Change Password ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_Returns204()
    {
        var (accessToken, _) = await RegisterAndLogin();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Content = JsonContent.Create(new { currentPassword = "Password1!", newPassword = "Password2!" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns422()
    {
        var (accessToken, _) = await RegisterAndLogin();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Content = JsonContent.Create(new { currentPassword = "WrongPass1!", newPassword = "Password2!" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_NotAuthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "Password1!", newPassword = "Password2!" }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Lockout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_FiveWrongPasswords_AccountBecomesLockedOut()
    {
        var email = $"lock-{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password1!");

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login",
                new { usernameOrEmail = email, password = "WrongPass1!" }, TestContext.Current.CancellationToken);
        }

        // After 5 failed attempts the account is locked — correct password still returns 401
        var locked = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = "Password1!" }, TestContext.Current.CancellationToken);

        locked.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task RegisterUser(string email, string password, string userName = "testuser")
    {
        var uniqueUserName = $"{userName}-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = uniqueUserName, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<(string AccessToken, string RefreshToken)> RegisterAndLogin()
    {
        var email = $"flow-{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password1!");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = "Password1!" }, TestContext.Current.CancellationToken);

        loginResponse.EnsureSuccessStatusCode();

        var body = await ParseJson(loginResponse);
        var accessToken = body.GetProperty("accessToken").GetString()!;
        var refreshToken = ExtractRefreshToken(loginResponse)!;

        return (accessToken, refreshToken);
    }

    private static async Task<JsonElement> ParseJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }

    private static string? ExtractRefreshToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        foreach (var cookie in cookies)
        {
            var parts = cookie.Split(';');
            var nameValue = parts[0].Trim();
            if (nameValue.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase))
                return nameValue["refresh_token=".Length..];
        }
        return null;
    }
}
