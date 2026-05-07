using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.CreatePermission;

public sealed class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IExecutionContext _executionContext;
    private readonly PermissionMapper _permissionMapper;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IExecutionContext executionContext,
        PermissionMapper permissionMapper)
    {
        _permissionRepository = permissionRepository;
        _executionContext = executionContext;
        _permissionMapper = permissionMapper;
    }

    public async Task<PermissionDto> Handle(CreatePermissionCommand request, CancellationToken ct)
    {
        if (await _permissionRepository.CodeExistsAsync(request.Code, ct))
            throw new ConflictException($"Permission code '{request.Code}' is already in use.");

        var permission = new Permission(Guid.NewGuid(), request.Code, request.Name);
        _permissionRepository.Add(permission);
        await _executionContext.CommitAsync(ct);

        return _permissionMapper.ToDto(permission);
    }
}
