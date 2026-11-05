using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMe;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Users;

public sealed class GetMeQueryHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UserMapper _mapper = new();

    private GetMeQueryHandler CreateHandler() => new(_repo, _currentUser, _mapper);

    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "me@test.com", "me", "hash");
        _currentUser.UserId.Returns(userId);
        _repo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetMeQuery(), CancellationToken.None);

        result.Id.ShouldBe(userId);
        result.Email.ShouldBe("me@test.com");
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsForbiddenException()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Should.ThrowAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new GetMeQuery(), CancellationToken.None));
    }
}
