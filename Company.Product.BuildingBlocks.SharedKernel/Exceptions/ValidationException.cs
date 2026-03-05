namespace Company.Product.BuildingBlocks.SharedKernel.Exceptions;

public sealed class ValidationException : DomainException
{
    public IDictionary<string, string[]>? Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors;
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
