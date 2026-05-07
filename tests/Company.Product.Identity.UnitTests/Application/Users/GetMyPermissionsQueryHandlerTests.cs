using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Queries.Users.GetMyPermissions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Users;

public sealed class GetMyPermissionsQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

    private GetMyPermissionsQueryHandler CreateHandler() => new(_currentUser, _permissionService);

    [Fact]
    public async Task Handle_ReturnsPermissionsSorted()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _permissionService.GetPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "users.write", "roles.read", "users.read" });

        var result = await CreateHandler().Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        result.ShouldBe(new[] { "roles.read", "users.read", "users.write" });
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsForbiddenException()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Should.ThrowAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new GetMyPermissionsQuery(), CancellationToken.None));
    }
}
