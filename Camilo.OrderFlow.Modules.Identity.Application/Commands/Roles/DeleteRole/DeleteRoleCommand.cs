using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Roles.DeleteRole;

public sealed record DeleteRoleCommand(Guid Id) : IRequest<Unit>;
