using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Queries.Roles.GetRoleById;

public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
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
