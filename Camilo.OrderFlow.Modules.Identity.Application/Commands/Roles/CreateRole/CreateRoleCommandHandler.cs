using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;
using Camilo.OrderFlow.Modules.Identity.Application.Mapping;
using Camilo.OrderFlow.Modules.Identity.Domain.Entities;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.CreateRole;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly RoleMapper _roleMapper;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IIdentityExecutionContext executionContext,
        RoleMapper roleMapper)
    {
        _roleRepository = roleRepository;
        _executionContext = executionContext;
        _roleMapper = roleMapper;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        if (await _roleRepository.NameExistsAsync(request.Name, ct))
            throw new ConflictException($"Role name '{request.Name}' is already in use.");

        var role = new Role(Guid.NewGuid(), request.Name, request.Description);
        _roleRepository.Add(role);
        await _executionContext.CommitAsync(ct);

        return _roleMapper.ToDto(role);
    }
}
