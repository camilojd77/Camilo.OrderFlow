using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Queries.Users.GetMe;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly UserMapper _userMapper;

    public GetMeQueryHandler(IUserRepository userRepository, ICurrentUser currentUser, UserMapper userMapper)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _userMapper = userMapper;
    }

    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        return _userMapper.ToDto(user);
    }
}
