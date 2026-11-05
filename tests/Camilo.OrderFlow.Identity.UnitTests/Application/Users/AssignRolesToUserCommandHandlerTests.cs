using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.AssignRolesToUser;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Users;

public sealed class AssignRolesToUserCommandHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
    private readonly IIdentityExecutionContext _ctx = Substitute.For<IIdentityExecutionContext>();

    private AssignRolesToUserCommandHandler CreateHandler()
        => new(_userRepo, _roleRepo, _permissionService, _ctx);

    [Fact]
    public async Task Handle_ValidRoles_AssignsAndInvalidatesCache()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User(userId, "u@test.com", "u", "hash");
        var role = new Role(roleId, "Editor");
        _userRepo.GetByIdWithRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role> { role });
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new AssignRolesToUserCommand(userId, new[] { roleId }),
            CancellationToken.None);

        result.ShouldBe(Unit.Value);
        user.UserRoles.Count.ShouldBe(1);
        await _permissionService.Received(1).InvalidateUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        _userRepo.GetByIdWithRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignRolesToUserCommand(Guid.NewGuid(), Array.Empty<Guid>()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "u@test.com", "u", "hash");
        _userRepo.GetByIdWithRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _roleRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Role>()); // empty — role not found

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new AssignRolesToUserCommand(userId, new[] { Guid.NewGuid() }),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyRoles_ClearsAllRoles()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "u@test.com", "u", "hash");
        _userRepo.GetByIdWithRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        await CreateHandler().Handle(
            new AssignRolesToUserCommand(userId, Array.Empty<Guid>()),
            CancellationToken.None);

        user.UserRoles.Count.ShouldBe(0);
    }
}
