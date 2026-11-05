using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;
