using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMe;

public sealed record GetMeQuery : IRequest<UserDto>;
