using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.CreatePermission;

public sealed record CreatePermissionCommand(string Code, string Name) : IRequest<PermissionDto>;
