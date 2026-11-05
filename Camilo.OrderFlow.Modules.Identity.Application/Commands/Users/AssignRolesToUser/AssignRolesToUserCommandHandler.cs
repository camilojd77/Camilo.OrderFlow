using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.AssignRolesToUser;

internal sealed class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionService _permissionService;
    private readonly IIdentityExecutionContext _executionContext;

    public AssignRolesToUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionService permissionService,
        IIdentityExecutionContext executionContext)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionService = permissionService;
        _executionContext = executionContext;
    }

    public async Task<Unit> Handle(AssignRolesToUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        if (request.RoleIds.Count > 0)
        {
            var foundRoles = await _roleRepository.GetByIdsAsync(request.RoleIds, ct);
            if (foundRoles.Count != request.RoleIds.Count)
                throw new NotFoundException("One or more roles not found.");
        }

        user.UserRoles.Clear();
        foreach (var roleId in request.RoleIds)
            user.UserRoles.Add(new UserRole(request.UserId, roleId));

        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        await _permissionService.InvalidateUserAsync(request.UserId, ct);

        return Unit.Value;
    }
}
