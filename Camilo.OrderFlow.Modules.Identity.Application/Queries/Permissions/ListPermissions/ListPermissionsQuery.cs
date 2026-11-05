using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Permissions.ListPermissions;

public sealed record ListPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;
