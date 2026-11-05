using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.DeactivateUser;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Users;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IIdentityExecutionContext _ctx = Substitute.For<IIdentityExecutionContext>();

    private DeactivateUserCommandHandler CreateHandler() => new(_repo, _ctx, TestTime.Provider);

    [Fact]
    public async Task Handle_RegularUser_DeactivatesAndReturnsUnit()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "u@test.com", "u", "hash", TestTime.UtcNow);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);
        _repo.IsOnlyActiveAdminAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new DeactivateUserCommand(id), CancellationToken.None);

        result.ShouldBe(Unit.Value);
        user.IsActive.ShouldBeFalse();
        _repo.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_LastAdmin_ThrowsConflictException()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "admin@test.com", "admin", "hash", TestTime.UtcNow);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);
        _repo.IsOnlyActiveAdminAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new DeactivateUserCommand(id), CancellationToken.None));

        _repo.DidNotReceive().Update(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
