using FluentValidation;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.UpdateUserProfile;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.AvatarUrl).MaximumLength(500).When(x => x.AvatarUrl is not null);
    }
}
