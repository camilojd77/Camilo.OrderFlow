namespace Company.Product.BuildingBlocks.SharedKernel.Exceptions;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }

    public ForbiddenException() : base("Access to the requested resource is forbidden.") { }
}
