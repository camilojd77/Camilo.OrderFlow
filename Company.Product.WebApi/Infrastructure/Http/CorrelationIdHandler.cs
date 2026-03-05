using Company.Product.WebApi.Observability;

namespace Company.Product.WebApi.Infrastructure.Http;

internal sealed class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var correlationId = httpContext?.Items[Correlation.ItemKey]?.ToString()
            ?? httpContext?.TraceIdentifier;

        if (!string.IsNullOrEmpty(correlationId)
            && !request.Headers.Contains(Correlation.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(Correlation.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
