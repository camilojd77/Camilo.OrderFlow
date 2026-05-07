using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;
using Company.Product.Modules.Identity.Application.Dtos;
using Company.Product.Modules.Identity.Application.Mapping;
using Company.Product.Modules.Identity.Domain.Entities;

namespace Company.Product.Modules.Identity.Application.Commands.Roles.CreateRole;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IExecutionContext _executionContext;
    private readonly RoleMapper _roleMapper;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IExecutionContext executionContext,
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
