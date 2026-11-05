using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Domain;

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

    // ── Activate ────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_AfterDeactivate_RestoresIsActiveTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");
        user.Deactivate();

        user.Activate();

        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_SetsUpdatedAtUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.Activate();

        user.UpdatedAtUtc.ShouldNotBeNull();
        user.UpdatedAtUtc!.Value.ShouldBeGreaterThan(before);
    }

    // ── RegisterFailedLogin ─────────────────────────────────────────────────

    [Fact]
    public void RegisterFailedLogin_BelowThreshold_IncrementsCounterAndDoesNotLock()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.FailedLoginAttempts.ShouldBe(1);
        user.LockoutEndUtc.ShouldBeNull();
        user.IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void RegisterFailedLogin_AtThreshold_LocksAccount()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");
        var lockoutDuration = TimeSpan.FromMinutes(15);
        var before = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: lockoutDuration);

        user.FailedLoginAttempts.ShouldBe(5);
        user.LockoutEndUtc.ShouldNotBeNull();
        user.LockoutEndUtc!.Value.ShouldBeGreaterThan(before.Add(lockoutDuration).AddSeconds(-2));
        user.IsLockedOut.ShouldBeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.UpdatedAtUtc.ShouldNotBeNull();
    }

    // ── ResetFailedLoginAttempts ────────────────────────────────────────────

    [Fact]
    public void ResetFailedLoginAttempts_ClearsCounterAndLockout()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");
        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        user.ResetFailedLoginAttempts();

        user.FailedLoginAttempts.ShouldBe(0);
        user.LockoutEndUtc.ShouldBeNull();
        user.IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void ResetFailedLoginAttempts_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.ResetFailedLoginAttempts();

        user.UpdatedAtUtc.ShouldNotBeNull();
    }

    // ── IsLockedOut ─────────────────────────────────────────────────────────

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsInFuture_IsTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.RegisterFailedLogin(maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));

        user.IsLockedOut.ShouldBeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsInPast_IsFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.RegisterFailedLogin(maxAttempts: 1, lockoutDuration: TimeSpan.FromMilliseconds(-1));

        user.IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenNeverLocked_IsFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash");

        user.IsLockedOut.ShouldBeFalse();
    }
}
