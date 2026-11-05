using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly UserMapper _userMapper;

    public GetUserByIdQueryHandler(IUserRepository userRepository, UserMapper userMapper)
    {
        _userRepository = userRepository;
        _userMapper = userMapper;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("User", request.Id);

        return _userMapper.ToDto(user);
    }
}
