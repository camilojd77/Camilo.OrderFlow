using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityExecutionContext _executionContext;

    public DeactivateUserCommandHandler(IUserRepository userRepository, IIdentityExecutionContext executionContext)
    {
        _userRepository = userRepository;
        _executionContext = executionContext;
    }

    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        if (await _userRepository.IsOnlyActiveAdminAsync(request.UserId, ct))
            throw new ConflictException("Cannot deactivate the last active admin user.");

        user.Deactivate();
        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
