using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.DeletePermission;

public sealed class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, Unit>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityExecutionContext _executionContext;

    public DeletePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IIdentityExecutionContext executionContext)
    {
        _permissionRepository = permissionRepository;
        _executionContext = executionContext;
    }

    public async Task<Unit> Handle(DeletePermissionCommand request, CancellationToken ct)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Permission {request.Id} not found.");

        if (await _permissionRepository.IsAssignedToAnyRoleAsync(request.Id, ct))
            throw new ConflictException($"Permission '{permission.Code}' is assigned to one or more roles and cannot be deleted.");

        _permissionRepository.Remove(permission);
        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
