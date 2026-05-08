using Company.Product.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Domain;

public sealed class PermissionTests
{
    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AssignsId()
    {
        var id = Guid.NewGuid();

        var permission = new Permission(id, "users.read", "View users");

        permission.Id.ShouldBe(id);
    }

    [Fact]
    public void Constructor_NormalizesCode_ToLowerInvariant()
    {
        var permission = new Permission(Guid.NewGuid(), "USERS.READ", "View users");

        permission.Code.ShouldBe("users.read");
    }

    [Fact]
    public void Constructor_TrimsCode()
    {
        var permission = new Permission(Guid.NewGuid(), "  users.read  ", "View users");

        permission.Code.ShouldBe("users.read");
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "  View users  ");

        permission.Name.ShouldBe("View users");
    }

    [Fact]
    public void Constructor_DoesNotLowercaseName()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "View Users");

        permission.Name.ShouldBe("View Users");
    }

    [Fact]
    public void Constructor_InitializesEmptyRolePermissions()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "View users");

        permission.RolePermissions.ShouldNotBeNull();
        permission.RolePermissions.ShouldBeEmpty();
    }

    // ── UpdateName ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateName_ReplacesName()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "old");

        permission.UpdateName("new name");

        permission.Name.ShouldBe("new name");
    }

    [Fact]
    public void UpdateName_TrimsName()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "old");

        permission.UpdateName("  new name  ");

        permission.Name.ShouldBe("new name");
    }

    [Fact]
    public void UpdateName_DoesNotChangeCode()
    {
        var permission = new Permission(Guid.NewGuid(), "users.read", "old");

        permission.UpdateName("new name");

        permission.Code.ShouldBe("users.read");
    }
}
