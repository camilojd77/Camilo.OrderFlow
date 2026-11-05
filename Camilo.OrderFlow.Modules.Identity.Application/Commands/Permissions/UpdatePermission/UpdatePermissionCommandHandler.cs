using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.UpdatePermission;

internal sealed class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, PermissionDto>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly PermissionMapper _permissionMapper;

    public UpdatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IIdentityExecutionContext executionContext,
        PermissionMapper permissionMapper)
    {
        _permissionRepository = permissionRepository;
        _executionContext = executionContext;
        _permissionMapper = permissionMapper;
    }

    public async Task<PermissionDto> Handle(UpdatePermissionCommand request, CancellationToken ct)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Permission {request.Id} not found.");

        permission.UpdateName(request.Name);
        _permissionRepository.Update(permission);
        await _executionContext.CommitAsync(ct);

        return _permissionMapper.ToDto(permission);
    }
}
