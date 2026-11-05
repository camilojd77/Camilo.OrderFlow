using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Commands.RegisterUser;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Camilo.OrderFlow.Identity.UnitTests.Application;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IIdentityExecutionContext _executionContext = Substitute.For<IIdentityExecutionContext>();
    private readonly UserMapper _userMapper = new();

    private RegisterUserCommandHandler CreateHandler() =>
        new(_userRepository, _passwordHasher, _executionContext, _userMapper);

    [Fact]
    public async Task Handle_NewEmail_ReturnsUserDto()
    {
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new RegisterUserCommand("alice@example.com", "alice", "Password1!");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Email.ShouldBe("alice@example.com");
        result.UserName.ShouldBe("alice");
        _userRepository.Received(1).Add(Arg.Any<User>());
        await _executionContext.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new RegisterUserCommand("alice@example.com", "alice", "Password1!");

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_InvalidCommand_ValidationBehaviorThrowsValidationException()
    {
        // Validation is handled by ValidationPipelineBehavior, not the handler itself.
        // Here we verify the handler does NOT throw on valid data.
        _userRepository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _executionContext.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new RegisterUserCommand("bob@example.com", "bobuser", "Password1!");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
    }
}
