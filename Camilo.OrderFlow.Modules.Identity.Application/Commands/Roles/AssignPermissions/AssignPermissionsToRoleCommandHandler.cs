using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.AssignPermissions;

public sealed class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand, Unit>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly IPermissionService _permissionService;

    public AssignPermissionsToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IIdentityExecutionContext executionContext,
        IPermissionService permissionService)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _executionContext = executionContext;
        _permissionService = permissionService;
    }

    public async Task<Unit> Handle(AssignPermissionsToRoleCommand request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, ct)
            ?? throw new NotFoundException($"Role {request.RoleId} not found.");

        if (request.PermissionIds.Count > 0)
        {
            var found = await _permissionRepository.GetByIdsAsync(request.PermissionIds, ct);
            if (found.Count != request.PermissionIds.Count)
                throw new NotFoundException("One or more permission IDs do not exist.");
        }

        role.RolePermissions.Clear();
        foreach (var permId in request.PermissionIds)
            role.RolePermissions.Add(new RolePermission(role.Id, permId));

        await _executionContext.CommitAsync(ct);
        await _permissionService.InvalidateRoleAsync(request.RoleId, ct);

        return Unit.Value;
    }
}
