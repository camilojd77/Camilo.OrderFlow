using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using FluentValidation;
using DomainValidationException = Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions.ValidationException;

namespace Camilo.OrderFlow.BuildingBlocks.Infrastructure.Validation;

/// <summary>
/// Runs every FluentValidation validator registered for the request before its handler and turns failures into a
/// <see cref="DomainValidationException"/> (400). Registered once in <c>AddBuildingBlocks</c> for all modules; each
/// module only registers its validators.
/// Validators run one after another: they may use scoped services such as a module's DbContext, which does not
/// support concurrent operations.
/// </summary>
internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            failures.AddRange(result.Errors.Where(failure => failure is not null));
        }

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());

            throw new DomainValidationException(errors);
        }

        return await next(ct);
    }
}
