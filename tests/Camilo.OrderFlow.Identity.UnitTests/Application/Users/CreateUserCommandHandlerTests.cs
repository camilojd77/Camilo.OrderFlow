using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.CreateUser;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application.Users;

public sealed class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IIdentityExecutionContext _ctx = Substitute.For<IIdentityExecutionContext>();
    private readonly UserMapper _mapper = new();

    private CreateUserCommandHandler CreateHandler() => new(_repo, _hasher, _ctx, _mapper, TestTime.Provider);

    [Fact]
    public async Task Handle_UniqueEmailAndUsername_ReturnsDto()
    {
        _repo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.UserNameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _ctx.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(
            new CreateUserCommand("new@test.com", "newuser", "Pass1!", "John", "Doe"),
            CancellationToken.None);

        result.ShouldNotBeNull();
        result.Email.ShouldBe("new@test.com");
        _repo.Received(1).Add(Arg.Any<User>());
        await _ctx.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        _repo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(
                new CreateUserCommand("dup@test.com", "user", "Pass1!", "A", "B"),
                CancellationToken.None));

        _repo.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsConflictException()
    {
        _repo.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.UserNameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(
                new CreateUserCommand("ok@test.com", "takenuser", "Pass1!", "A", "B"),
                CancellationToken.None));

        _repo.DidNotReceive().Add(Arg.Any<User>());
    }
}
