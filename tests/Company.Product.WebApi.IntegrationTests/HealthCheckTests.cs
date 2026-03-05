using FluentAssertions;
using System.Net;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests;

/// <summary>
/// Smoke tests against the health endpoints.
/// Uses <see cref="WebApiFactory"/> which replaces EF Core with InMemory
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
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealthLive_ResponseBody_ContainsHealthyStatus()
    {
        var response = await _client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetHealthLive_ResponseContainsSecurityHeaders()
    {
        var response = await _client.GetAsync("/health/live");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task GetHealthReady_Returns200Or503_NotCrash()
    {
        // /health/ready may return 503 if SQL Server is unavailable in the test environment;
        // what matters is that the endpoint exists and returns a well-formed response.
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}
