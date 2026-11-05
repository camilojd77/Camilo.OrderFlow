using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Permissions.CreatePermission;

internal sealed class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionDto>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly PermissionMapper _permissionMapper;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IIdentityExecutionContext executionContext,
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
