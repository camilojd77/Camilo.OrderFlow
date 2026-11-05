using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = TestTime.UtcNow;

    private static RefreshToken Create(
        DateTime? expiresAtUtc = null,
        Guid? id = null,
        Guid? userId = null,
        string tokenHash = "hash",
        string createdByIp = "127.0.0.1") =>
        new(
            id ?? Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            tokenHash,
            expiresAtUtc ?? Now.AddDays(7),
            createdByIp);

    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AssignsAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expiresAt = Now.AddDays(7);

        var token = new RefreshToken(id, userId, "hash", expiresAt, "10.0.0.1");

        token.Id.ShouldBe(id);
        token.UserId.ShouldBe(userId);
        token.TokenHash.ShouldBe("hash");
        token.ExpiresAtUtc.ShouldBe(expiresAt);
        token.CreatedByIp.ShouldBe("10.0.0.1");
    }

    [Fact]
    public void Constructor_LeavesRevocationFieldsEmpty()
    {
        var token = Create();

        token.RevokedAtUtc.ShouldBeNull();
        token.ReplacedByTokenId.ShouldBeNull();
    }

    // ── IsExpired ───────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_WhenExpiresAtUtcInFuture_IsFalse()
    {
        var token = Create(expiresAtUtc: Now.AddMinutes(5));

        token.IsExpired(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtUtcInPast_IsTrue()
    {
        var token = Create(expiresAtUtc: Now.AddMinutes(-5));

        token.IsExpired(Now).ShouldBeTrue();
    }

    // ── IsRevoked ───────────────────────────────────────────────────────────

    [Fact]
    public void IsRevoked_NewToken_IsFalse()
    {
        var token = Create();

        token.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void IsRevoked_AfterRevoke_IsTrue()
    {
        var token = Create();

        token.Revoke(Now);

        token.IsRevoked.ShouldBeTrue();
    }

    // ── IsActive ────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_NewTokenInFuture_IsTrue()
    {
        var token = Create(expiresAtUtc: Now.AddMinutes(5));

        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ExpiredToken_IsFalse()
    {
        var token = Create(expiresAtUtc: Now.AddMinutes(-5));

        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_RevokedToken_IsFalse()
    {
        var token = Create(expiresAtUtc: Now.AddMinutes(5));
        token.Revoke(Now);

        token.IsActive(Now).ShouldBeFalse();
    }

    // ── Revoke ──────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_SetsRevokedAtUtc()
    {
        var token = Create();
        var revokedAt = Now;

        token.Revoke(revokedAt);

        token.RevokedAtUtc.ShouldBe(revokedAt);
    }

    [Fact]
    public void Revoke_WithReplacement_SetsReplacedByTokenId()
    {
        var token = Create();
        var replacement = Guid.NewGuid();

        token.Revoke(Now, replacement);

        token.ReplacedByTokenId.ShouldBe(replacement);
    }

    [Fact]
    public void Revoke_WithoutReplacement_LeavesReplacedByTokenIdNull()
    {
        var token = Create();

        token.Revoke(Now);

        token.ReplacedByTokenId.ShouldBeNull();
    }
}
