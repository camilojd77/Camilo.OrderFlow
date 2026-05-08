using Company.Product.BuildingBlocks.SharedKernel.Exceptions;

namespace Company.Product.Modules.Identity.Domain.ValueObjects;

public sealed record UserProfile
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }

    private UserProfile() { } // EF Core

    public UserProfile(
        string firstName,
        string lastName,
        string? phoneNumber = null,
        string? avatarUrl = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ValidationException(new Dictionary<string, string[]>
            { [nameof(FirstName)] = ["First name is required."] });

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ValidationException(new Dictionary<string, string[]>
            { [nameof(LastName)] = ["Last name is required."] });

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        AvatarUrl = avatarUrl?.Trim();
    }
}
