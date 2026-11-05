using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.AssignPermissions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Roles;

public sealed class AssignPermissionsToRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository _permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

    private AssignPermissionsToRoleCommandHandler CreateHandler() =>
        new(_roleRepository, _permissionRepository, _executionContext, _permissionService);

    [Fact]
    public async Task Handle_ValidPermissions_ReplacesAndReturnsUnit()
    {
        var roleId = Guid.NewGuid();
        var permId1 = Guid.NewGuid();
        var permId2 = Guid.NewGuid();

        var role = new Role(roleId, "Editor", null);
        var permissions = new List<Permission>
        {
            new(permId1, "articles.read", "Read articles"),
            new(permId2, "articles.write", "Write articles"),
        };

        _roleRepository.GetByIdWithPermissionsAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _permissionRepository.GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(permissions);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new AssignPermissionsToRoleCommand(roleId, new[] { permId1, permId2 });
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldBe(Unit.Value);
        role.RolePermissions.Count.ShouldBe(2);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyPermissions_ClearsAll()
    {
        var roleId = Guid.NewGuid();
        var role = new Role(roleId, "Viewer", null);
        _roleRepository.GetByIdWithPermissionsAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new AssignPermissionsToRoleCommand(roleId, Array.Empty<Guid>());
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldBe(Unit.Value);
        role.RolePermissions.ShouldBeEmpty();
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFoundException()
    {
        _roleRepository.GetByIdWithPermissionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Role?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignPermissionsToRoleCommand(Guid.NewGuid(), new[] { Guid.NewGuid() }),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidPermissionId_ThrowsNotFoundException()
    {
        var roleId = Guid.NewGuid();
        var role = new Role(roleId, "Editor", null);
        _roleRepository.GetByIdWithPermissionsAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
        // Returns fewer permissions than requested → one ID is invalid
        _permissionRepository.GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Permission>());

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignPermissionsToRoleCommand(roleId, new[] { Guid.NewGuid() }),
                CancellationToken.None));
    }
}
