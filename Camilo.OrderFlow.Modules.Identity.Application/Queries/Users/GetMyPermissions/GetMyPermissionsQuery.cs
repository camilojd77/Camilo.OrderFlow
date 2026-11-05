using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMyPermissions;

public sealed record GetMyPermissionsQuery : IRequest<IReadOnlyList<string>>;
