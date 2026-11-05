using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using System.Collections.Concurrent;

namespace Camilo.OrderFlow.BuildingBlocks.Infrastructure.Messaging;

internal sealed class Sender : ISender
{
    // Cached per request type; creating the generic dispatcher via reflection is done only once.
    private static readonly ConcurrentDictionary<Type, HandlerBase> _cache = new();
    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var wrapper = _cache.GetOrAdd(request.GetType(), requestType =>
            (HandlerBase)Activator.CreateInstance(
                typeof(RequestDispatcher<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        var result = await wrapper.Handle(request, _serviceProvider, ct);
        return (TResponse)result!;
    }
}

internal abstract class HandlerBase
{
    public abstract Task<object?> Handle(object request, IServiceProvider sp, CancellationToken ct);
}

internal sealed class RequestDispatcher<TRequest, TResponse> : HandlerBase
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider sp, CancellationToken ct)
    {
        var typed = (TRequest)request;

        var handler = sp.GetService(typeof(IRequestHandler<TRequest, TResponse>))
            as IRequestHandler<TRequest, TResponse>
            ?? throw new InvalidOperationException(
                $"No handler registered for '{typeof(TRequest).Name}'. " +
                $"Ensure it is registered via AddIdentityApplication() or the corresponding module's DI extension.");

        var behaviors = (sp.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>))
            as IEnumerable<IPipelineBehavior<TRequest, TResponse>>
            ?? Enumerable.Empty<IPipelineBehavior<TRequest, TResponse>>())
            .Reverse()
            .ToArray();

        RequestHandlerDelegate<TResponse> pipeline = token => handler.Handle(typed, token);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var b = behavior;
            pipeline = token => b.Handle(typed, next, token);
        }

        return await pipeline(ct);
    }
}
