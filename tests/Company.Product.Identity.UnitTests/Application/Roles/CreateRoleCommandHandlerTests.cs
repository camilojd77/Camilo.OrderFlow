using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Commands.Roles.CreateRole;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Roles;

public sealed class CreateRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly RoleMapper _roleMapper = new();

    private CreateRoleCommandHandler CreateHandler() =>
        new(_roleRepository, _executionContext, _roleMapper);

    [Fact]
    public async Task Handle_UniqueName_ReturnsRoleDto()
    {
        _roleRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new CreateRoleCommand("Manager", "Project manager");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Manager");
        result.Description.ShouldBe("Project manager");
        _roleRepository.Received(1).Add(Arg.Any<Role>());
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflictException()
    {
        _roleRepository.NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new CreateRoleCommand("Admin", null), CancellationToken.None));

        _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
    }
}
