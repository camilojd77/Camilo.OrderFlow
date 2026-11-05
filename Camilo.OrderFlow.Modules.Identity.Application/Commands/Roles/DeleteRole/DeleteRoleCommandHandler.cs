using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Unit>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly IPermissionService _permissionService;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityExecutionContext executionContext,
        IPermissionService permissionService)
    {
        _roleRepository = roleRepository;
        _executionContext = executionContext;
        _permissionService = permissionService;
    }

    public async Task<Unit> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Role {request.Id} not found.");

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("The 'Admin' role cannot be deleted.");

        _roleRepository.Remove(role);
        await _executionContext.CommitAsync(ct);
        await _permissionService.InvalidateRoleAsync(role.Id, ct);

        return Unit.Value;
    }
}
