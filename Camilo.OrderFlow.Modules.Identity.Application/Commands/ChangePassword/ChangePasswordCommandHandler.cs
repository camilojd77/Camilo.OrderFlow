using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Commands.ChangePassword;

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityExecutionContext _executionContext;
    private readonly TimeProvider _timeProvider;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        IIdentityExecutionContext executionContext,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _executionContext = executionContext;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Unauthenticated request reached ChangePasswordCommandHandler.");

        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException(new Dictionary<string, string[]>
            { ["CurrentPassword"] = ["Current password is incorrect."] });

        user.UpdatePasswordHash(_passwordHasher.Hash(request.NewPassword), _timeProvider.GetUtcNow().UtcDateTime);
        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
