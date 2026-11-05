using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTime Now = TestTime.UtcNow;
    private static readonly DateTime Later = Now.AddMinutes(10);

    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NormalizesEmail_ToLowerInvariant()
    {
        var user = new User(Guid.NewGuid(), "ADMIN@Example.COM", "admin", "hash", Now);

        user.Email.ShouldBe("admin@example.com");
    }

    [Fact]
    public void Constructor_TrimsEmail()
    {
        var user = new User(Guid.NewGuid(), "  admin@example.com  ", "admin", "hash", Now);

        user.Email.ShouldBe("admin@example.com");
    }

    [Fact]
    public void Constructor_TrimsUserName()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "  admin  ", "hash", Now);

        user.UserName.ShouldBe("admin");
    }

    [Fact]
    public void Constructor_SetsIsActiveTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_SetsCreatedAtUtc_ToGivenTime()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.CreatedAtUtc.ShouldBe(Now);
        user.UpdatedAtUtc.ShouldBeNull();
    }

    // ── Deactivate ──────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.Deactivate(Now);

        user.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.Deactivate(Later);

        user.UpdatedAtUtc.ShouldBe(Later);
    }

    [Fact]
    public void Deactivate_CalledTwice_RemainsInactive()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.Deactivate(Now);
        user.Deactivate(Now);

        user.IsActive.ShouldBeFalse();
    }

    // ── UpdatePasswordHash ──────────────────────────────────────────────────

    [Fact]
    public void UpdatePasswordHash_ReplacesHash()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "old-hash", Now);

        user.UpdatePasswordHash("new-hash", Later);

        user.PasswordHash.ShouldBe("new-hash");
    }

    [Fact]
    public void UpdatePasswordHash_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "old-hash", Now);

        user.UpdatePasswordHash("new-hash", Later);

        user.UpdatedAtUtc.ShouldBe(Later);
    }

    // ── Activate ────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_AfterDeactivate_RestoresIsActiveTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);
        user.Deactivate(Now);

        user.Activate(Later);

        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.Activate(Later);

        user.UpdatedAtUtc.ShouldBe(Later);
    }

    // ── RegisterFailedLogin ─────────────────────────────────────────────────

    [Fact]
    public void RegisterFailedLogin_BelowThreshold_IncrementsCounterAndDoesNotLock()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: Now);

        user.FailedLoginAttempts.ShouldBe(1);
        user.LockoutEndUtc.ShouldBeNull();
        user.IsLockedOut(Now).ShouldBeFalse();
    }

    [Fact]
    public void RegisterFailedLogin_AtThreshold_LocksAccount()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);
        var lockoutDuration = TimeSpan.FromMinutes(15);

        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: lockoutDuration, utcNow: Now);

        user.FailedLoginAttempts.ShouldBe(5);
        user.LockoutEndUtc.ShouldBe(Now.Add(lockoutDuration));
        user.IsLockedOut(Now).ShouldBeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: Later);

        user.UpdatedAtUtc.ShouldBe(Later);
    }

    // ── ResetFailedLoginAttempts ────────────────────────────────────────────

    [Fact]
    public void ResetFailedLoginAttempts_ClearsCounterAndLockout()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);
        for (var i = 0; i < 5; i++)
            user.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: Now);

        user.ResetFailedLoginAttempts(Later);

        user.FailedLoginAttempts.ShouldBe(0);
        user.LockoutEndUtc.ShouldBeNull();
        user.IsLockedOut(Now).ShouldBeFalse();
    }

    [Fact]
    public void ResetFailedLoginAttempts_SetsUpdatedAtUtc()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.ResetFailedLoginAttempts(Later);

        user.UpdatedAtUtc.ShouldBe(Later);
    }

    // ── IsLockedOut ─────────────────────────────────────────────────────────

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsInFuture_IsTrue()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.RegisterFailedLogin(maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: Now);

        user.IsLockedOut(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndIsInPast_IsFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.RegisterFailedLogin(maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: Now);

        user.IsLockedOut(Now.AddMinutes(16)).ShouldBeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenNeverLocked_IsFalse()
    {
        var user = new User(Guid.NewGuid(), "admin@example.com", "admin", "hash", Now);

        user.IsLockedOut(Now).ShouldBeFalse();
    }
}
