using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Queries.Users.GetMyPermissions;

public sealed record GetMyPermissionsQuery : IRequest<IReadOnlyList<string>>;
