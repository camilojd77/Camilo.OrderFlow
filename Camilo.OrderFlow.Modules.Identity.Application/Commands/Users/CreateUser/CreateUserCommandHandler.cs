using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;
using Camilo.OrderFlow.Modules.Identity.Domain.ValueObjects;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly UserMapper _userMapper;
    private readonly TimeProvider _timeProvider;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IIdentityExecutionContext executionContext,
        UserMapper userMapper,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _executionContext = executionContext;
        _userMapper = userMapper;
        _timeProvider = timeProvider;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("The email is already registered.");

        if (await _userRepository.UserNameExistsAsync(request.UserName, ct))
            throw new ConflictException("The username is already taken.");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var user = new User(
            Guid.NewGuid(),
            request.Email,
            request.UserName,
            _passwordHasher.Hash(request.Password),
            utcNow);

        user.UpdateProfile(new UserProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.AvatarUrl),
            utcNow);

        _userRepository.Add(user);
        await _executionContext.CommitAsync(ct);

        return _userMapper.ToDto(user);
    }
}
