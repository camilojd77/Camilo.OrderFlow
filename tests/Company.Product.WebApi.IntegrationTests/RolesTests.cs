using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests;

public sealed class RolesTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public RolesTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/roles ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_AsAdmin_Returns200WithList()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet("/api/roles", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/roles", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/roles/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_AdminRole_Returns200()
    {
        var token = await LoginAsAdminAsync();
        var adminRoleId = await GetAdminRoleIdAsync(token);

        var request = AuthorizedGet($"/api/roles/{adminRoleId}", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("name").GetString().ShouldBe("Admin");
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var token = await LoginAsAdminAsync();
        var request = AuthorizedGet($"/api/roles/{Guid.NewGuid()}", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST /api/roles ──────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidData_Returns201()
    {
        var token = await LoginAsAdminAsync();
        var name = $"Role-{Guid.NewGuid():N}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/roles");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name, description = "Test role" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var token = await LoginAsAdminAsync();

        // Admin role already seeded — try to create it again
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/roles");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name = "Admin", description = (string?)null });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── PUT /api/roles/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingRole_Returns200WithUpdatedData()
    {
        var token = await LoginAsAdminAsync();
        var roleId = await CreateRoleAsync(token, $"Upd-{Guid.NewGuid():N}");
        var newName = $"Updated-{Guid.NewGuid():N}";

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{roleId}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name = newName, description = "Updated" });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("name").GetString().ShouldBe(newName);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var token = await LoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{Guid.NewGuid()}");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name = "X", description = (string?)null });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/roles/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task Delete_NonAdminRole_Returns204()
    {
        var token = await LoginAsAdminAsync();
        var roleId = await CreateRoleAsync(token, $"Del-{Guid.NewGuid():N}");

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{roleId}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AdminRole_Returns409()
    {
        var token = await LoginAsAdminAsync();
        var adminRoleId = await GetAdminRoleIdAsync(token);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{adminRoleId}");
        request.Headers.Authorization = new("Bearer", token);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── PUT /api/roles/{id}/permissions ─────────────────────────────────────

    [Fact]
    public async Task AssignPermissions_EmptySet_Returns204()
    {
        var token = await LoginAsAdminAsync();
        var roleId = await CreateRoleAsync(token, $"Perm-{Guid.NewGuid():N}");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{roleId}/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { permissionIds = Array.Empty<Guid>() });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignPermissions_UnknownRole_Returns404()
    {
        var token = await LoginAsAdminAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/roles/{Guid.NewGuid()}/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { permissionIds = Array.Empty<Guid>() });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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

    private async Task<Guid> GetAdminRoleIdAsync(string token)
    {
        var request = AuthorizedGet("/api/roles", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var roles = await ParseJsonAsync(response);
        foreach (var role in roles.EnumerateArray())
        {
            if (role.GetProperty("name").GetString() == "Admin")
                return role.GetProperty("id").GetGuid();
        }
        throw new InvalidOperationException("Admin role not found in seeded data.");
    }

    private async Task<Guid> CreateRoleAsync(string token, string name)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/roles");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { name, description = (string?)null });
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("id").GetGuid();
    }

    private static HttpRequestMessage AuthorizedGet(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }
}
