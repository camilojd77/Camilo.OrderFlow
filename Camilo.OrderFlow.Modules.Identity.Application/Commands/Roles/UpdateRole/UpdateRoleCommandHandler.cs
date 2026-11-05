using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.UpdateRole;

internal sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly RoleMapper _roleMapper;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityExecutionContext executionContext,
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
