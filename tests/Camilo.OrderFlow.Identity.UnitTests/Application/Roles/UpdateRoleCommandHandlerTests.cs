using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.UpdateRole;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Roles;

public sealed class UpdateRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly RoleMapper _roleMapper = new();

    private UpdateRoleCommandHandler CreateHandler() =>
        new(_roleRepository, _executionContext, _roleMapper);

    [Fact]
    public async Task Handle_ValidUpdate_ReturnsUpdatedDto()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "OldName", null);
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(role);
        _roleRepository.GetByNameAsync("NewName", Arg.Any<CancellationToken>()).Returns((Role?)null);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new UpdateRoleCommand(id, "NewName", "Updated"), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("NewName");
        result.Description.ShouldBe("Updated");
        _roleRepository.Received(1).Update(role);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _roleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new UpdateRoleCommand(Guid.NewGuid(), "X", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NameTakenByOtherRole_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var role = new Role(id, "Original", null);
        var other = new Role(otherId, "Taken", null);
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(role);
        _roleRepository.GetByNameAsync("Taken", Arg.Any<CancellationToken>()).Returns(other);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateRoleCommand(id, "Taken", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SameNameSameRole_Succeeds()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "Admin", "Administrator");
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(role);
        // GetByName returns the same role — not a conflict
        _roleRepository.GetByNameAsync("Admin", Arg.Any<CancellationToken>()).Returns(role);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new UpdateRoleCommand(id, "Admin", "Updated desc"), CancellationToken.None);

        result.Name.ShouldBe("Admin");
        result.Description.ShouldBe("Updated desc");
    }
}
