using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Queries.Roles.ListRoles;

public sealed class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly RoleMapper _roleMapper;

    public ListRolesQueryHandler(IRoleRepository roleRepository, RoleMapper roleMapper)
    {
        _roleRepository = roleRepository;
        _roleMapper = roleMapper;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(ListRolesQuery request, CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync(ct);
        return roles.Select(_roleMapper.ToDto).ToList();
    }
}
