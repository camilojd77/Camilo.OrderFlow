namespace Company.Product.BuildingBlocks.SharedKernel.Messaging;

public interface IRequest<TResponse> { }

public interface IRequest : IRequest<Unit> { }

public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
