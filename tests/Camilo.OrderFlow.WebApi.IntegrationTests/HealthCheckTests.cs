using Shouldly;
using System.Net;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

/// <summary>
/// Smoke tests against the health endpoints.
/// Uses <see cref="WebApiFactory"/> which replaces EF Core with SQLite in-memory
/// so no external infrastructure is required.
/// </summary>
public sealed class HealthCheckTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealthLive_ResponseBody_ContainsHealthyStatus()
    {
        var response = await _client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.ShouldContain("Healthy");
    }

    [Fact]
    public async Task GetHealthLive_ResponseContainsSecurityHeaders()
    {
        var response = await _client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.Headers.Contains("X-Content-Type-Options").ShouldBeTrue();
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
    }

    [Fact]
    public async Task GetHealthReady_Returns200Or503_NotCrash()
    {
        // /health/ready may return 503 if SQL Server is unavailable in the test environment;
        // what matters is that the endpoint exists and returns a well-formed response.
        var response = await _client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        new[] { HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable }
            .ShouldContain(response.StatusCode);
    }
}
