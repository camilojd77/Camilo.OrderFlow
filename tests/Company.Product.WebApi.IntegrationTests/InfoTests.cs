using Shouldly;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Company.Product.WebApi.IntegrationTests;

public sealed class InfoTests : IClassFixture<WebApiFactory>
{
    private readonly HttpClient _client;

    public InfoTests(WebApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /_info ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInfo_Returns200WithExpectedShape()
    {
        var response = await _client.GetAsync("/_info");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(json).RootElement;

        body.GetProperty("serviceName").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("environment").GetString().ShouldBe("Testing");
        body.GetProperty("uptimeSeconds").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
    }
}
