using Camilo.OrderFlow.WebApi.Observability;
using Serilog.Context;
using System.Diagnostics;

namespace Camilo.OrderFlow.WebApi.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context.Request.Headers);

        context.Items[Correlation.ItemKey] = correlationId;
        Activity.Current?.SetTag("correlation_id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Correlation.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(Correlation.ItemKey, correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(IHeaderDictionary headers)
        => headers.TryGetValue(Correlation.HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
