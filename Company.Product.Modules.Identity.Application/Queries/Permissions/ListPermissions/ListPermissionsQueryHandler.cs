using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Queries.Permissions.ListPermissions;

public sealed class ListPermissionsQueryHandler : IRequestHandler<ListPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly PermissionMapper _permissionMapper;

    public ListPermissionsQueryHandler(IPermissionRepository permissionRepository, PermissionMapper permissionMapper)
    {
        _permissionRepository = permissionRepository;
        _permissionMapper = permissionMapper;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(ListPermissionsQuery request, CancellationToken ct)
    {
        var permissions = await _permissionRepository.GetAllAsync(ct);
        return permissions.Select(_permissionMapper.ToDto).ToList();
    }
}
