namespace Company.Product.BuildingBlocks.SharedKernel.Messaging;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct = default);

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
