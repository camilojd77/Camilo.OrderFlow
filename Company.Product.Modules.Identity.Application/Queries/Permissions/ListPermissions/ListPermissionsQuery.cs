using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.Modules.Identity.Application.Dtos;

namespace Company.Product.Modules.Identity.Application.Queries.Permissions.ListPermissions;

public sealed record ListPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;
