using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests.RealDb;

/// <summary>
/// docs/adr/0029-revocacion-de-sesiones.md: the same refresh token presented by several requests at once is consumed
/// by exactly one of them. Needs real SQL Server: the race depends on the database, not on SQLite in memory.
/// </summary>
[Trait("Category", "RealDb")]
[Collection(nameof(SqlServerCollection))]
public sealed class RefreshTokenConcurrencyTests(SqlServerFixture fixture)
{
    private const int ConcurrentRequests = 8;
    private const string Password = "Password1!";
    private readonly HttpClient _client = fixture.Factory.CreateClient(new() { HandleCookies = false });

    [Fact]
    public async Task Refresh_SameTokenInParallel_OnlyOneSucceeds()
    {
        var refreshToken = await RegisterAndLoginAsync();

        using var start = new SemaphoreSlim(0, ConcurrentRequests);
        var requests = Enumerable.Range(0, ConcurrentRequests).Select(async _ =>
        {
            await start.WaitAsync(TestContext.Current.CancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
            var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            return response.StatusCode;
        }).ToList();
        start.Release(ConcurrentRequests);

        var statuses = await Task.WhenAll(requests);

        statuses.Count(status => status == HttpStatusCode.OK).ShouldBe(1,
            "Only one request may rotate the token. Statuses: " + string.Join(", ", statuses));
        statuses.Count(status => status == HttpStatusCode.Unauthorized).ShouldBe(ConcurrentRequests - 1);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"race-{Guid.NewGuid():N}@test.com";
        (await _client.PostAsJsonAsync("/api/auth/register",
            new { email, userName = $"race-{Guid.NewGuid():N}"[..20], password = Password },
            TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { usernameOrEmail = email, password = Password }, TestContext.Current.CancellationToken);
        login.EnsureSuccessStatusCode();

        return login.Headers.GetValues("Set-Cookie")
            .Select(cookie => cookie.Split(';')[0])
            .Where(pair => pair.StartsWith("refresh_token=", StringComparison.Ordinal))
            .Select(pair => pair["refresh_token=".Length..])
            .First();
    }
}
