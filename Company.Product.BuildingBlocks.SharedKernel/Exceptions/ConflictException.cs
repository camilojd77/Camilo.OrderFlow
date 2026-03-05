namespace Company.Product.BuildingBlocks.SharedKernel.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
