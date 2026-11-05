using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
