using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using FluentValidation;
using DomainValidationException = Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions.ValidationException;

namespace Camilo.OrderFlow.Modules.Identity.Application.Behaviors;

public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next(ct);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray());

            throw new DomainValidationException(errors);
        }

        return await next(ct);
    }
}
