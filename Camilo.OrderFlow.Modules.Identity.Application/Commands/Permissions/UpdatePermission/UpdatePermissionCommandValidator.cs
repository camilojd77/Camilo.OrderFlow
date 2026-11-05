using FluentValidation;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.UpdatePermission;

public sealed class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);
    }
}
