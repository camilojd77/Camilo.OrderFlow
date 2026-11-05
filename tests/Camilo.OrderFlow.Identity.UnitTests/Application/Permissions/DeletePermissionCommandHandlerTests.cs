using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.DeletePermission;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Permissions;

public sealed class DeletePermissionCommandHandlerTests
{
    private readonly IPermissionRepository _repo = Substitute.For<IPermissionRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();

    private DeletePermissionCommandHandler CreateHandler() => new(_repo, _executionContext);

    [Fact]
    public async Task Handle_UnassignedPermission_DeletesAndReturnsUnit()
    {
        var id = Guid.NewGuid();
        var permission = new Permission(id, "orders.read", "Read orders");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(permission);
        _repo.IsAssignedToAnyRoleAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new DeletePermissionCommand(id), CancellationToken.None);

        result.ShouldBe(Unit.Value);
        _repo.Received(1).Remove(permission);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Permission?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeletePermissionCommand(Guid.NewGuid()), CancellationToken.None));

        _repo.DidNotReceive().Remove(Arg.Any<Permission>());
    }

    [Fact]
    public async Task Handle_AssignedPermission_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var permission = new Permission(id, "roles.read", "Read roles");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(permission);
        _repo.IsAssignedToAnyRoleAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new DeletePermissionCommand(id), CancellationToken.None));

        _repo.DidNotReceive().Remove(Arg.Any<Permission>());
    }
}
