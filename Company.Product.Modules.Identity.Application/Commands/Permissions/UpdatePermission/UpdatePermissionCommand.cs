using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid Id, string Name) : IRequest<PermissionDto>;
