using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Roles.ListRoles;

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
