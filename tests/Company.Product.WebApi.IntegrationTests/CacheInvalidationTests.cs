using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests;

public sealed class CacheInvalidationTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public CacheInvalidationTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Cache invalidation via role permission assignment ────────────────────

    [Fact]
    public async Task AssignPermissions_InvalidatesCache_UserSeesNewPermissionImmediately()
    {
        var adminToken = await LoginAsAdminAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];

        // Create permission, role, user
        var permId = await CreatePermissionAsync(adminToken, $"cache.perm{uid}", $"Cache Perm {uid}");
        var roleId = await CreateRoleAsync(adminToken, $"CacheRole{uid}");
        var userId = await CreateUserAsync(adminToken, $"cachetest{uid}@test.com", $"cachetest{uid}", "Test@1234!");

        // Assign the role to the user
        var assignRoleResp = await _client.SendAsync(AuthorizedPut(
            $"/api/users/{userId}/roles",
            new { roleIds = new[] { roleId } },
            adminToken), TestContext.Current.CancellationToken);
        assignRoleResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Login as the new user — first call primes the permission cache
        var userToken = await LoginAsync($"cachetest{uid}@test.com", "Test@1234!");
        var permsBefore = await GetMyPermissionsAsync(userToken);
        permsBefore.ShouldNotContain($"cache.perm{uid}",
            "permission should not exist before assignment");

        // Admin assigns the permission to the role — must invalidate cache for this role
        var assignPermResp = await _client.SendAsync(AuthorizedPut(
            $"/api/roles/{roleId}/permissions",
            new { permissionIds = new[] { permId } },
            adminToken), TestContext.Current.CancellationToken);
        assignPermResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Same user token — permissions should now include the new code
        var permsAfter = await GetMyPermissionsAsync(userToken);
        permsAfter.ShouldContain($"cache.perm{uid}",
            "permission cache should have been invalidated when role was updated");
    }

    [Fact]
    public async Task DeleteRole_InvalidatesCache_UserLosesRolePermissions()
    {
        var adminToken = await LoginAsAdminAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];

        // Create permission, role, user
        var permId = await CreatePermissionAsync(adminToken, $"del.perm{uid}", $"Del Perm {uid}");
        var roleId = await CreateRoleAsync(adminToken, $"DelRole{uid}");
        var userId = await CreateUserAsync(adminToken, $"deltest{uid}@test.com", $"deltest{uid}", "Test@1234!");

        // Assign permission to role, role to user
        var assignPermResp = await _client.SendAsync(AuthorizedPut(
            $"/api/roles/{roleId}/permissions",
            new { permissionIds = new[] { permId } },
            adminToken), TestContext.Current.CancellationToken);
        assignPermResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var assignRoleResp = await _client.SendAsync(AuthorizedPut(
            $"/api/users/{userId}/roles",
            new { roleIds = new[] { roleId } },
            adminToken), TestContext.Current.CancellationToken);
        assignRoleResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Prime the cache
        var userToken = await LoginAsync($"deltest{uid}@test.com", "Test@1234!");
        var permsBefore = await GetMyPermissionsAsync(userToken);
        permsBefore.ShouldContain($"del.perm{uid}", "permission should be present before role deletion");

        // Remove the role from the user first (can't delete a role with active assignments unless cascade)
        var removeRoleResp = await _client.SendAsync(AuthorizedPut(
            $"/api/users/{userId}/roles",
            new { roleIds = Array.Empty<Guid>() },
            adminToken), TestContext.Current.CancellationToken);
        removeRoleResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Delete the role — must invalidate cache for that role
        var deleteRoleResp = new HttpRequestMessage(HttpMethod.Delete, $"/api/roles/{roleId}");
        deleteRoleResp.Headers.Authorization = new("Bearer", adminToken);
        var deleteResp = await _client.SendAsync(deleteRoleResp, TestContext.Current.CancellationToken);
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // User no longer has the role: permissions should not include the old code
        var permsAfter = await GetMyPermissionsAsync(userToken);
        permsAfter.ShouldNotContain($"del.perm{uid}",
            "permission cache should have been invalidated when role was deleted");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> LoginAsAdminAsync() => await LoginAsync("admin@company.com", "Admin123!");

    private async Task<string> LoginAsync(string usernameOrEmail, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail, password }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<List<string>> GetMyPermissionsAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/permissions");
        request.Headers.Authorization = new("Bearer", token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    private async Task<Guid> CreatePermissionAsync(string token, string code, string name)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new { code, name, description = (string?)null });
        var resp = await _client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(resp);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateRoleAsync(string token, string name)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/roles");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new { name, description = (string?)null });
        var resp = await _client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(resp);
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateUserAsync(string token, string email, string userName, string password)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(new { email, userName, password, firstName = "Cache", lastName = "Test" });
        var resp = await _client.SendAsync(req, TestContext.Current.CancellationToken);
        resp.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(resp);
        return body.GetProperty("id").GetGuid();
    }

    private static HttpRequestMessage AuthorizedPut(string url, object body, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, url);
        req.Headers.Authorization = new("Bearer", token);
        req.Content = JsonContent.Create(body);
        return req;
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement;
    }
}
