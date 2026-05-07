using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.UpdateRole;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IExecutionContext _executionContext;
    private readonly RoleMapper _roleMapper;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IExecutionContext executionContext,
        RoleMapper roleMapper)
    {
        _roleRepository = roleRepository;
        _executionContext = executionContext;
        _roleMapper = roleMapper;
    }

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Role {request.Id} not found.");

        var nameHolder = await _roleRepository.GetByNameAsync(request.Name, ct);
        if (nameHolder is not null && nameHolder.Id != request.Id)
            throw new ConflictException($"Role name '{request.Name}' is already in use.");

        role.Update(request.Name, request.Description);
        _roleRepository.Update(role);
        await _executionContext.CommitAsync(ct);

        return _roleMapper.ToDto(role);
    }
}
