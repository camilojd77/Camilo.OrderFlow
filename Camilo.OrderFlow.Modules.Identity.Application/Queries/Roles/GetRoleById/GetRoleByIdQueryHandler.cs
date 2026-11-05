using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Roles.GetRoleById;

internal sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly RoleMapper _roleMapper;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository, RoleMapper roleMapper)
    {
        _roleRepository = roleRepository;
        _roleMapper = roleMapper;
    }

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Role {request.Id} not found.");

        return _roleMapper.ToDto(role);
    }
}
