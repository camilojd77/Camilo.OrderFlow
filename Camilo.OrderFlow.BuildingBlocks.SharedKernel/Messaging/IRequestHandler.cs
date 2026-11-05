namespace Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
