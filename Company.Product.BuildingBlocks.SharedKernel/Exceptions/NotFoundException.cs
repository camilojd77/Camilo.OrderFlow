namespace Company.Product.BuildingBlocks.SharedKernel.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.") { }
}
