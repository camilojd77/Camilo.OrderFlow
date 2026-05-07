using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Application.Queries.Roles.GetRoleById;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Roles;

public sealed class GetRoleByIdQueryHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly RoleMapper _roleMapper = new();

    private GetRoleByIdQueryHandler CreateHandler() => new(_roleRepository, _roleMapper);

    [Fact]
    public async Task Handle_ExistingId_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "Editor", "Content editor");
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await CreateHandler().Handle(new GetRoleByIdQuery(id), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
        result.Name.ShouldBe("Editor");
        result.Description.ShouldBe("Content editor");
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _roleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
