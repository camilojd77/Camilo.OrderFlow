using Company.Product.BuildingBlocks.SharedKernel.Messaging;

namespace Company.Product.Modules.Identity.Application.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
