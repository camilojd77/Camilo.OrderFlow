using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.ValueObjects;

namespace Company.Product.Modules.Identity.Application.Commands.Users.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly IExecutionContext _executionContext;
    private readonly UserMapper _userMapper;

    public UpdateUserProfileCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IPermissionService permissionService,
        IExecutionContext executionContext,
        UserMapper userMapper)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _executionContext = executionContext;
        _userMapper = userMapper;
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
            request.AvatarUrl));

        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return _userMapper.ToDto(user);
    }
}
