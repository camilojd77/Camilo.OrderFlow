using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.UpdatePermission;

public sealed class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, PermissionDto>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IExecutionContext _executionContext;
    private readonly PermissionMapper _permissionMapper;

    public UpdatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IExecutionContext executionContext,
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
