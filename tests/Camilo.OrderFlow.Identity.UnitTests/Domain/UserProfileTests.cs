using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Domain;

public sealed class UserProfileTests
{
    // ── Constructor — valid data ────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidData_CreatesProfile()
    {
        var profile = new UserProfile("Alice", "Smith", "+1234567890", "https://example.com/avatar.png");

        profile.FirstName.ShouldBe("Alice");
        profile.LastName.ShouldBe("Smith");
        profile.PhoneNumber.ShouldBe("+1234567890");
        profile.AvatarUrl.ShouldBe("https://example.com/avatar.png");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var profile = new UserProfile("  Alice  ", "  Smith  ");

        profile.FirstName.ShouldBe("Alice");
        profile.LastName.ShouldBe("Smith");
    }

    [Fact]
    public void Constructor_NullableFields_AllowNull()
    {
        var profile = new UserProfile("Alice", "Smith");

        profile.PhoneNumber.ShouldBeNull();
        profile.AvatarUrl.ShouldBeNull();
    }

    // ── Constructor — validation ────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceFirstName_ThrowsValidationException(string firstName)
    {
        Should.Throw<ValidationException>(() =>
            new UserProfile(firstName, "Smith"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceLastName_ThrowsValidationException(string lastName)
    {
        Should.Throw<ValidationException>(() =>
            new UserProfile("Alice", lastName));
    }

    [Fact]
    public void Constructor_ValidationException_ContainsFieldName()
    {
        var ex = Should.Throw<ValidationException>(() =>
            new UserProfile("", "Smith"));

        ex.Errors.ShouldNotBeNull();
        ex.Errors!.ContainsKey("FirstName").ShouldBeTrue();
    }

    // ── Value equality (record semantics) ───────────────────────────────────

    [Fact]
    public void TwoProfiles_WithSameData_AreEqual()
    {
        var a = new UserProfile("Alice", "Smith");
        var b = new UserProfile("Alice", "Smith");

        a.ShouldBe(b);
    }

    [Fact]
    public void TwoProfiles_WithDifferentData_AreNotEqual()
    {
        var a = new UserProfile("Alice", "Smith");
        var b = new UserProfile("Bob", "Smith");

        a.ShouldNotBe(b);
    }
}

public sealed class UserUpdateProfileTests
{
    private static User CreateUser() =>
        new(Guid.NewGuid(), "user@example.com", "username", "hash");

    [Fact]
    public void UpdateProfile_SetsProfile()
    {
        var user = CreateUser();
        var profile = new UserProfile("Alice", "Smith");

        user.UpdateProfile(profile);

        user.Profile.ShouldNotBeNull();
        user.Profile!.FirstName.ShouldBe("Alice");
        user.Profile.LastName.ShouldBe("Smith");
    }

    [Fact]
    public void UpdateProfile_SetsUpdatedAtUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = CreateUser();

        user.UpdateProfile(new UserProfile("Alice", "Smith"));

        user.UpdatedAtUtc.ShouldNotBeNull();
        user.UpdatedAtUtc!.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public void UpdateProfile_ReplacesExistingProfile()
    {
        var user = CreateUser();
        user.UpdateProfile(new UserProfile("Alice", "Smith"));

        user.UpdateProfile(new UserProfile("Bob", "Jones"));

        user.Profile!.FirstName.ShouldBe("Bob");
    }

    [Fact]
    public void UpdateProfile_NullProfile_ThrowsArgumentNullException()
    {
        var user = CreateUser();

        Should.Throw<ArgumentNullException>(() =>
            user.UpdateProfile(null!));
    }

    [Fact]
    public void NewUser_ProfileIsNull()
    {
        var user = CreateUser();

        user.Profile.ShouldBeNull();
    }
}
