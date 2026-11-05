using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Dtos;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string UserName, string Password) : IRequest<UserDto>;
