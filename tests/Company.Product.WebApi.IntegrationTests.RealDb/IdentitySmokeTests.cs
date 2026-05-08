using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests.RealDb;

/// <summary>
/// Smoke tests against a real SQL Server 2022 instance booted by Testcontainers.
/// The goal is to catch model/migration drift that an SQLite-based suite cannot
/// — e.g. SQL Server-specific column types, unique-index collation, owned-entity
/// column collisions, cascade behavior. We exercise the read path, the write path,
/// the unique constraint, the migration chain, and the authentication round-trip.
///
/// All tests are tagged [Trait("Category","RealDb")] so they're excluded from the
/// fast run via `dotnet test --filter "Category!=RealDb"` and included in CI nightly
/// via `dotnet test --filter "Category=RealDb"`.
/// </summary>
[Trait("Category", "RealDb")]
[Collection(nameof(SqlServerCollection))]
public sealed class IdentitySmokeTests
{
    private readonly HttpClient _client;

    public IdentitySmokeTests(SqlServerFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    // ── Migrations + seed ────────────────────────────────────────────────────

    [Fact]
    public async Task Health_AfterMigrationsApplied_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Auth happy + error ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_AsSeededAdmin_Returns200WithAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "Admin123!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ParseJsonAsync(response);
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "WrongPass1!" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Write path + unique constraint enforced by SQL Server ────────────────

    [Fact]
    public async Task CreateRole_HappyPath_PersistsAndIsReadable()
    {
        var token = await LoginAsAdminAsync();
        var name = $"RealDb-{Guid.NewGuid():N}";

        var create = new HttpRequestMessage(HttpMethod.Post, "/api/roles");
        create.Headers.Authorization = new("Bearer", token);
        create.Content = JsonContent.Create(new { name, description = "smoke" });

        var createResp = await _client.SendAsync(create);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await ParseJsonAsync(createResp);
        var id = created.GetProperty("id").GetGuid();

        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/roles/{id}");
        get.Headers.Authorization = new("Bearer", token);
        var getResp = await _client.SendAsync(get);

        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await ParseJsonAsync(getResp);
        fetched.GetProperty("name").GetString().ShouldBe(name);
    }

    [Fact]
    public async Task CreatePermission_DuplicateCode_Returns409()
    {
        var token = await LoginAsAdminAsync();

        // "users.read" is seeded — re-creating it must trip the SQL Server unique index.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/permissions");
        request.Headers.Authorization = new("Bearer", token);
        request.Content = JsonContent.Create(new { code = "users.read", name = "duplicate" });

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ── Owned-entity (UserProfile) round-trip — exercises OwnsOne column mapping ──

    [Fact]
    public async Task CreateUser_WithProfile_RoundTripsThroughSqlServer()
    {
        var token = await LoginAsAdminAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var create = new HttpRequestMessage(HttpMethod.Post, "/api/users");
        create.Headers.Authorization = new("Bearer", token);
        create.Content = JsonContent.Create(new
        {
            email = $"realdb{suffix}@test.com",
            userName = $"realdb{suffix}",
            password = "Pass123!",
            firstName = "Real",
            lastName = "Db"
        });

        var createResp = await _client.SendAsync(create);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await ParseJsonAsync(createResp);
        var id = created.GetProperty("id").GetGuid();

        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{id}");
        get.Headers.Authorization = new("Bearer", token);
        var getResp = await _client.SendAsync(get);

        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var fetched = await ParseJsonAsync(getResp);
        fetched.GetProperty("profile").GetProperty("firstName").GetString().ShouldBe("Real");
        fetched.GetProperty("profile").GetProperty("lastName").GetString().ShouldBe("Db");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> LoginAsAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = "admin@company.com", password = "Admin123!" });
        response.EnsureSuccessStatusCode();
        var body = await ParseJsonAsync(response);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
