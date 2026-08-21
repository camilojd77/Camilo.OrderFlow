using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly UserMapper _userMapper;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IIdentityExecutionContext executionContext,
        UserMapper userMapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _executionContext = executionContext;
        _userMapper = userMapper;
    }

    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var user = new User(Guid.NewGuid(), request.Email, request.UserName, _passwordHasher.Hash(request.Password));
        _userRepository.Add(user);
        await _executionContext.CommitAsync(ct);

        return _userMapper.ToDto(user);
    }
}
