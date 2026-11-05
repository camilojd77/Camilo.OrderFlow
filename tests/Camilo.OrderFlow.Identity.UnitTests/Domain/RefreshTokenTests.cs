using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Domain;

public sealed class RefreshTokenTests
{
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
            expiresAtUtc ?? DateTime.UtcNow.AddDays(7),
            createdByIp);

    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AssignsAllFields()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

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
        var token = Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(5));

        token.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtUtcInPast_IsTrue()
    {
        var token = Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(-5));

        token.IsExpired.ShouldBeTrue();
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

        token.Revoke(DateTime.UtcNow);

        token.IsRevoked.ShouldBeTrue();
    }

    // ── IsActive ────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_NewTokenInFuture_IsTrue()
    {
        var token = Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(5));

        token.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ExpiredToken_IsFalse()
    {
        var token = Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(-5));

        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_RevokedToken_IsFalse()
    {
        var token = Create(expiresAtUtc: DateTime.UtcNow.AddMinutes(5));
        token.Revoke(DateTime.UtcNow);

        token.IsActive.ShouldBeFalse();
    }

    // ── Revoke ──────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_SetsRevokedAtUtc()
    {
        var token = Create();
        var revokedAt = DateTime.UtcNow;

        token.Revoke(revokedAt);

        token.RevokedAtUtc.ShouldBe(revokedAt);
    }

    [Fact]
    public void Revoke_WithReplacement_SetsReplacedByTokenId()
    {
        var token = Create();
        var replacement = Guid.NewGuid();

        token.Revoke(DateTime.UtcNow, replacement);

        token.ReplacedByTokenId.ShouldBe(replacement);
    }

    [Fact]
    public void Revoke_WithoutReplacement_LeavesReplacedByTokenIdNull()
    {
        var token = Create();

        token.Revoke(DateTime.UtcNow);

        token.ReplacedByTokenId.ShouldBeNull();
    }
}
