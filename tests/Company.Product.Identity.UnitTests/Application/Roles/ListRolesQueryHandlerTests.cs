using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Queries.Roles.ListRoles;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Roles;

public sealed class ListRolesQueryHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly RoleMapper _roleMapper = new();

    private ListRolesQueryHandler CreateHandler() => new(_roleRepository, _roleMapper);

    [Fact]
    public async Task Handle_RolesExist_ReturnsMappedDtos()
    {
        var roles = new List<Role>
        {
            new(Guid.NewGuid(), "Admin", "System administrator"),
            new(Guid.NewGuid(), "User", null),
        };
        _roleRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(roles);

        var result = await CreateHandler().Handle(new ListRolesQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Admin");
        result[1].Name.ShouldBe("User");
    }

    [Fact]
    public async Task Handle_NoRoles_ReturnsEmptyList()
    {
        _roleRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Role>());

        var result = await CreateHandler().Handle(new ListRolesQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
