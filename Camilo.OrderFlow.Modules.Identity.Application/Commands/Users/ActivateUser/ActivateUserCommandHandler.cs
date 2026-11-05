using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.ActivateUser;

internal sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly TimeProvider _timeProvider;

    public ActivateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityExecutionContext executionContext,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _executionContext = executionContext;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        user.Activate(_timeProvider.GetUtcNow().UtcDateTime);
        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
