using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.DeleteRole;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Roles;

public sealed class DeleteRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

    private DeleteRoleCommandHandler CreateHandler() => new(_roleRepository, _executionContext, _permissionService);

    [Fact]
    public async Task Handle_ExistingNonAdminRole_DeletesAndReturnsUnit()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "Editor", null);
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(role);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new DeleteRoleCommand(id), CancellationToken.None);

        result.ShouldBe(Unit.Value);
        _roleRepository.Received(1).Remove(role);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _roleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteRoleCommand(Guid.NewGuid()), CancellationToken.None));

        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
    }

    [Fact]
    public async Task Handle_AdminRole_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var adminRole = new Role(id, "Admin", "System administrator");
        _roleRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(adminRole);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new DeleteRoleCommand(id), CancellationToken.None));

        _roleRepository.DidNotReceive().Remove(Arg.Any<Role>());
    }
}
