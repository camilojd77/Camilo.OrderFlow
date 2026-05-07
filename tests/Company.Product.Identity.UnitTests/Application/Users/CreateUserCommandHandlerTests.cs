using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Commands.Users.CreateUser;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application.Users;

public sealed class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IExecutionContext _ctx = Substitute.For<IExecutionContext>();
    private readonly UserMapper _mapper = new();

    private CreateUserCommandHandler CreateHandler() => new(_repo, _hasher, _ctx, _mapper);

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
