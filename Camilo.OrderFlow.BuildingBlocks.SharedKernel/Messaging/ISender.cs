namespace Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}
