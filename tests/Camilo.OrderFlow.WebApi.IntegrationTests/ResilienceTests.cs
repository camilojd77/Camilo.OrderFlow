using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests;

/// <summary>
/// The resilient ErpClient retries transient failures only for safe HTTP methods; writes are sent once.
/// The real pipeline is used; only the primary handler is replaced by one that always answers 503.
/// </summary>
public sealed class ResilienceTests(WebApiFactory factory) : IClassFixture<WebApiFactory>
{
    private const string ClientName = "ErpClient";
    private const int ConfiguredRetries = 3; // Resilience:ErpClient:RetryCount in appsettings.json

    [Fact]
    public async Task Get_TransientFailure_IsRetried()
    {
        var (client, handler) = CreateClient();

        var response = await client.GetAsync("/orders", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Attempts.ShouldBe(1 + ConfiguredRetries);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task UnsafeMethod_TransientFailure_IsNotRetried(string method)
    {
        var (client, handler) = CreateClient();

        using var request = new HttpRequestMessage(new HttpMethod(method), "/orders");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Attempts.ShouldBe(1);
    }

    private (HttpClient Client, CountingHandler Handler) CreateClient()
    {
        var handler = new CountingHandler();
        var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient(ClientName).ConfigurePrimaryHttpMessageHandler(() => handler)));

        var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
        client.BaseAddress = new Uri("http://erp.test");
        return (client, handler);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _attempts);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }
}
