using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests;

public sealed class UsersTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public UsersTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/users ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithPagedResult()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet("/api/users?page=1&pageSize=10", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("items").ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetProperty("totalCount").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/users/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingUser_Returns200()
    {
        var token = await LoginAsAdminAsync();
        var adminId = await GetAdminUserIdAsync(token);
        var request = AuthorizedGet($"/api/users/{adminId}", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("id").GetGuid().ShouldBe(adminId);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet($"/api/users/{Guid.NewGuid()}", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST /api/users ──────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidData_Returns201()
    {
        var token = await LoginAsAdminAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new
        {
            email = $"newuser{suffix}@test.com",
            userName = $"newuser{suffix}",
            password = "Pass123!",
            firstName = "New",
            lastName = "User"
        });

        var response = await _client.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ParseJsonAsync(response);
        body.GetProperty("email").GetString().ShouldBe($"newuser{suffix}@test.com");
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var token = await LoginAsAdminAsync();

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new
        {
            email = "admin@company.com",
            userName = "anotheruser",
            password = "Pass123!",
            firstName = "A",
            lastName = "B"
        });

        var response = await _client.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── PUT /api/users/{id}/profile ──────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_OwnProfile_Returns200()
    {
        var token = await LoginAsAdminAsync();
        var adminId = await GetAdminUserIdAsync(token);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{adminId}/profile");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new { firstName = "Updated", lastName = "Admin", phoneNumber = (string?)null, avatarUrl = (string?)null });

        var response = await _client.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("profile").GetProperty("firstName").GetString().ShouldBe("Updated");
    }

    // ── POST /api/users/{id}/deactivate & activate ───────────────────────

    [Fact]
    public async Task Deactivate_ThenActivate_Returns204()
    {
        var token = await LoginAsAdminAsync();
        var userId = await CreateUserAndGetIdAsync(token);

        var deactivate = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/deactivate");
        deactivate.Headers.Authorization = new("Bearer", token);
        var deactivateResp = await _client.SendAsync(deactivate);
        deactivateResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var activate = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/activate");
        activate.Headers.Authorization = new("Bearer", token);
        var activateResp = await _client.SendAsync(activate);
        activateResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── PUT /api/users/{id}/roles ────────────────────────────────────────

    [Fact]
    public async Task AssignRoles_ValidRoles_Returns204()
    {
        var token = await LoginAsAdminAsync();
        var userId = await CreateUserAndGetIdAsync(token);
        var adminRoleId = await GetAdminRoleIdAsync(token);

        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}/roles");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new { roleIds = new[] { adminRoleId } });

        var response = await _client.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // ── GET /api/users/me ────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_Authenticated_Returns200()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet("/api/users/me", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("email").GetString().ShouldBe("admin@company.com");
    }

    [Fact]
    public async Task GetMe_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/users/me/permissions ────────────────────────────────────

    [Fact]
    public async Task GetMyPermissions_AsAdmin_ReturnsPermissionList()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet("/api/users/me/permissions", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBeGreaterThanOrEqualTo(6);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "Admin123!" });
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> GetAdminUserIdAsync(string token)
    {
        var request = AuthorizedGet("/api/users/me", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateUserAndGetIdAsync(string token)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new
        {
            email = $"temp{suffix}@test.com",
            userName = $"temp{suffix}",
            password = "Pass123!",
            firstName = "Temp",
            lastName = "User"
        });
        var response = await _client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> GetAdminRoleIdAsync(string token)
    {
        var request = AuthorizedGet("/api/roles", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var list = await ParseJsonAsync(response);
        foreach (var role in list.EnumerateArray())
        {
            if (role.GetProperty("name").GetString() == "Admin")
                return role.GetProperty("id").GetGuid();
        }
        throw new InvalidOperationException("Admin role not found.");
    }

    private static HttpRequestMessage AuthorizedGet(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new("Bearer", token);
        return req;
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
