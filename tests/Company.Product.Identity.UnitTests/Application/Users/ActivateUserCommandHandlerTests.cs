using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Commands.Users.ActivateUser;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Users;

public sealed class ActivateUserCommandHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IExecutionContext _ctx = Substitute.For<IExecutionContext>();

    private ActivateUserCommandHandler CreateHandler() => new(_repo, _ctx);

    [Fact]
    public async Task Handle_InactiveUser_ActivatesAndReturnsUnit()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "u@test.com", "u", "hash");
        user.Deactivate();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(user);
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new ActivateUserCommand(id), CancellationToken.None);

        result.ShouldBe(Unit.Value);
        user.IsActive.ShouldBeTrue();
        _repo.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_UnknownId_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(new ActivateUserCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
