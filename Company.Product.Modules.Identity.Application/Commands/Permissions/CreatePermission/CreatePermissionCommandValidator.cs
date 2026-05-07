using FluentValidation;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.CreatePermission;

public sealed class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z][a-z0-9_]*\.[a-z][a-z0-9_]*$")
            .WithMessage("Code must follow the format '<resource>.<action>' in lowercase (e.g. 'users.read').");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
