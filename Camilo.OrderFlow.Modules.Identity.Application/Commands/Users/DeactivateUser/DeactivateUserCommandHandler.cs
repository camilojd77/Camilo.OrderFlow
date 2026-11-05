using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.Users.DeactivateUser;

internal sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;

    public DeactivateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityExecutionContext executionContext,
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _executionContext = executionContext;
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        if (await _userRepository.IsOnlyActiveAdminAsync(request.UserId, ct))
            throw new ConflictException("Cannot deactivate the last active admin user.");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        user.Deactivate(utcNow);
        _userRepository.Update(user);

        // End every session: refresh tokens are revoked here and access tokens are rejected on the next request
        // because authentication checks that the user is still active.
        foreach (var refreshToken in await _refreshTokenRepository.GetActiveByUserAsync(user.Id, utcNow, ct))
            refreshToken.Revoke(utcNow);

        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
