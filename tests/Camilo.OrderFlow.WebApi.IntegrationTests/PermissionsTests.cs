using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

public sealed class PermissionsTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public PermissionsTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/permissions ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithSeededPermissions()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet("/api/permissions", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBeGreaterThanOrEqualTo(6); // at least the 6 seeded permissions
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/permissions", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/permissions ────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidData_Returns201()
    {
        var token = await LoginAsAdminAsync();
        var code = "test.perm" + Guid.NewGuid().ToString("N")[..8];

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { code, name = "Test permission" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await ParseJsonAsync(response);
        body.GetProperty("code").GetString().ShouldBe(code);
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns409()
    {
        var token = await LoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { code = "users.read", name = "Duplicate" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_InvalidCodeFormat_Returns400()
    {
        var token = await LoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { code = "INVALID_FORMAT", name = "Bad code" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/permissions/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingPermission_Returns200WithUpdatedName()
    {
        var token = await LoginAsAdminAsync();
        var permId = await CreatePermissionAndGetIdAsync(token, "upd.perm" + Guid.NewGuid().ToString("N")[..8], "Original");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/permissions/{permId}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name = "Updated name" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("name").GetString().ShouldBe("Updated name");
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var token = await LoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/permissions/{Guid.NewGuid()}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name = "X" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/permissions/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_UnassignedPermission_Returns204()
    {
        var token = await LoginAsAdminAsync();
        var permId = await CreatePermissionAndGetIdAsync(token, "del.perm" + Guid.NewGuid().ToString("N")[..8], "To delete");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/permissions/{permId}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AssignedPermission_Returns409()
    {
        var token = await LoginAsAdminAsync();
        // "users.read" is seeded and assigned to the Admin role
        var usersReadId = await GetPermissionIdByCodeAsync(token, "users.read");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/permissions/{usersReadId}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "Admin123!" }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> CreatePermissionAndGetIdAsync(string token, string code, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { code, name });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> GetPermissionIdByCodeAsync(string token, string code)
    {
        var request = AuthorizedGet("/api/permissions", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var list = await ParseJsonAsync(response);
        foreach (var p in list.EnumerateArray())
        {
            if (p.GetProperty("code").GetString() == code)
                return p.GetProperty("id").GetGuid();
        }
        throw new InvalidOperationException($"Permission '{code}' not found.");
    }

    private static HttpRequestMessage AuthorizedGet(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new("Bearer", token);
        return req;
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }
}
