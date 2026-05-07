using Company.Product.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Domain;

public sealed class UserTests
{
    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NormalizesEmail_ToLowerInvariant()
    {
        var user = new User(Guid.NewGuid(), "ADMIN@Example.COM", "admin", "hash");

        user.Email.ShouldBe("admin@example.com");
    }

    [Fact]
    public void Constructor_TrimsEmail()
    {
        var user = new User(Guid.NewGuid(), "  admin@example.com  ", "admin", "hash");

        user.Email.ShouldBe("admin@example.com");
    }

    [Fact]
    public void Constructor_TrimsUserName()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "  admin  ", "hash");

        user.UserName.ShouldBe("admin");
    }

    [Fact]
    public void Constructor_SetsIsActiveTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_SetsCreatedAtUtc_ToApproximatelyNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.CreatedAtUtc.ShouldBeGreaterThan(before);
        user.CreatedAtUtc.ShouldBeLessThan(DateTime.UtcNow.AddSeconds(1));
    }

    // ── Deactivate ──────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.Deactivate();

        user.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_SetsUpdatedAtUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.Deactivate();

        user.UpdatedAtUtc.ShouldNotBeNull();
        user.UpdatedAtUtc!.Value.ShouldBeGreaterThan(before);
    }

    [Fact]
    public void Deactivate_CalledTwice_RemainsInactive()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.Deactivate();
        user.Deactivate();

        user.IsActive.ShouldBeFalse();
    }

    // ── UpdatePasswordHash ──────────────────────────────────────────────────

    [Fact]
    public void UpdatePasswordHash_ReplacesHash()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "old-hash");

        user.UpdatePasswordHash("new-hash");

        user.PasswordHash.ShouldBe("new-hash");
    }

    [Fact]
    public void UpdatePasswordHash_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "old-hash");

        user.UpdatePasswordHash("new-hash");

        user.UpdatedAtUtc.ShouldNotBeNull();
    }
}
