using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMe;

internal sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserDto>
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
