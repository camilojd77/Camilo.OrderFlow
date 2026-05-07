using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.Permissions.DeletePermission;

public sealed record DeletePermissionCommand(Guid Id) : IRequest<Unit>;
