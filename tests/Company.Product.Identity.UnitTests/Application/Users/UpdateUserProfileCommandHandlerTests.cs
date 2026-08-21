using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Commands.Users.UpdateUserProfile;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Users;

public sealed class UpdateUserProfileCommandHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
    private readonly IIdentityExecutionContext _ctx = Substitute.For<IIdentityExecutionContext>();
    private readonly UserMapper _mapper = new();

    private UpdateUserProfileCommandHandler CreateHandler()
        => new(_repo, _currentUser, _permissionService, _ctx, _mapper);

    [Fact]
    public async Task Handle_OwnProfile_UpdatesWithoutPermissionCheck()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "u@test.com", "u", "hash");
        _currentUser.UserId.Returns(userId);
        _repo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new UpdateUserProfileCommand(userId, "Jane", "Doe", null, null),
            CancellationToken.None);

        result.Profile!.FirstName.ShouldBe("Jane");
        await _permissionService.DidNotReceive().GetPermissionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherUserWithPermission_UpdatesProfile()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var user = new User(targetId, "target@test.com", "target", "hash");
        _currentUser.UserId.Returns(callerId);
        _permissionService.GetPermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "users.write" });
        _repo.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(user);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new UpdateUserProfileCommand(targetId, "John", "Smith", null, null),
            CancellationToken.None);

        result.Profile!.FirstName.ShouldBe("John");
    }

    [Fact]
    public async Task Handle_OtherUserWithoutPermission_ThrowsForbiddenException()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _currentUser.UserId.Returns(callerId);
        _permissionService.GetPermissionsAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());

        await Should.ThrowAsync<ForbiddenException>(() =>
            CreateHandler().Handle(
                new UpdateUserProfileCommand(targetId, "X", "Y", null, null),
                CancellationToken.None));

        _repo.DidNotReceive().Update(Arg.Any<User>());
    }
}
