using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Commands.ChangePassword;
using Company.Product.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Company.Product.Identity.UnitTests.Application;

public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IExecutionContext _executionContext = Substitute.For<IExecutionContext>();

    private ChangePasswordCommandHandler CreateHandler() =>
        new(_userRepository, _passwordHasher, _currentUser, _executionContext);

    private static User MakeUser(string passwordHash = "correct-hash")
    {
        var user = new User(Guid.NewGuid(), "alice@example.com", "alice", passwordHash);
        return user;
    }

    [Fact]
    public async Task Handle_ValidCurrentPassword_ChangesPasswordAndReturnsUnit()
    {
        var user = MakeUser();
        _currentUser.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("OldPass1!", user.PasswordHash).Returns(true);
        _passwordHasher.Hash("NewPass1!").Returns("new-hash");
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ChangePasswordCommand("OldPass1!", "NewPass1!");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldBe(Unit.Value);
        user.PasswordHash.ShouldBe("new-hash");
        _userRepository.Received(1).Update(user);
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsValidationException()
    {
        var user = MakeUser();
        _currentUser.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPass!", user.PasswordHash).Returns(false);

        var command = new ChangePasswordCommand("WrongPass!", "NewPass1!");

        await Should.ThrowAsync<ValidationException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new ChangePasswordCommand("OldPass1!", "NewPass1!");

        await Should.ThrowAsync<NotFoundException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));
    }
}
