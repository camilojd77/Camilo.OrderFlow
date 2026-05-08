using Company.Product.Modules.Identity.Domain.Entities;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Domain;

public sealed class RoleTests
{
    // ── Constructor ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_AssignsId()
    {
        var id = Guid.NewGuid();

        var role = new Role(id, "Admin");

        role.Id.ShouldBe(id);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var role = new Role(Guid.NewGuid(), "  Admin  ");

        role.Name.ShouldBe("Admin");
    }

    [Fact]
    public void Constructor_TrimsDescription()
    {
        var role = new Role(Guid.NewGuid(), "Admin", "  Full access  ");

        role.Description.ShouldBe("Full access");
    }

    [Fact]
    public void Constructor_WithoutDescription_DescriptionIsNull()
    {
        var role = new Role(Guid.NewGuid(), "Admin");

        role.Description.ShouldBeNull();
    }

    [Fact]
    public void Constructor_NullDescription_KeepsNull()
    {
        var role = new Role(Guid.NewGuid(), "Admin", description: null);

        role.Description.ShouldBeNull();
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        var role = new Role(Guid.NewGuid(), "Admin");

        role.UserRoles.ShouldNotBeNull();
        role.UserRoles.ShouldBeEmpty();
        role.RolePermissions.ShouldNotBeNull();
        role.RolePermissions.ShouldBeEmpty();
    }

    // ── Update ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ReplacesNameAndDescription()
    {
        var role = new Role(Guid.NewGuid(), "Admin", "old");

        role.Update("Manager", "new");

        role.Name.ShouldBe("Manager");
        role.Description.ShouldBe("new");
    }

    [Fact]
    public void Update_TrimsName()
    {
        var role = new Role(Guid.NewGuid(), "Admin");

        role.Update("  Manager  ", null);

        role.Name.ShouldBe("Manager");
    }

    [Fact]
    public void Update_TrimsDescription()
    {
        var role = new Role(Guid.NewGuid(), "Admin");

        role.Update("Manager", "  desc  ");

        role.Description.ShouldBe("desc");
    }

    [Fact]
    public void Update_WithNullDescription_ClearsDescription()
    {
        var role = new Role(Guid.NewGuid(), "Admin", "old");

        role.Update("Admin", null);

        role.Description.ShouldBeNull();
    }
}
