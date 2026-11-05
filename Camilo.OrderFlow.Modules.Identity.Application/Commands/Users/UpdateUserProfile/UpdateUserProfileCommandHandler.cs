using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.ValueObjects;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly UserMapper _userMapper;
    private readonly TimeProvider _timeProvider;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IPermissionService permissionService,
        IIdentityExecutionContext executionContext,
        UserMapper userMapper,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _executionContext = executionContext;
        _userMapper = userMapper;
        _timeProvider = timeProvider;
    }

    public async Task<UserDto> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        var callerId = _currentUser.UserId ?? throw new ForbiddenException();

        if (request.UserId != callerId)
        {
            var perms = await _permissionService.GetPermissionsAsync(callerId, ct);
            if (!perms.Contains("users.write"))
                throw new ForbiddenException();
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        user.UpdateProfile(new UserProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.AvatarUrl),
            _timeProvider.GetUtcNow().UtcDateTime);

        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return _userMapper.ToDto(user);
    }
}
