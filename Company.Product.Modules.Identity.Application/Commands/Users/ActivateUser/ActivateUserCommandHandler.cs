using Company.Product.BuildingBlocks.SharedKernel.Exceptions;
using Company.Product.BuildingBlocks.SharedKernel.Messaging;
using Company.Product.BuildingBlocks.SharedKernel.Persistence;
using Company.Product.Modules.Identity.Application.Abstractions;

namespace Company.Product.Modules.Identity.Application.Commands.Users.ActivateUser;

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IExecutionContext _executionContext;

    public ActivateUserCommandHandler(IUserRepository userRepository, IExecutionContext executionContext)
    {
        _userRepository = userRepository;
        _executionContext = executionContext;
    }

    public async Task<Unit> Handle(ActivateUserCommand request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        user.Activate();
        _userRepository.Update(user);
        await _executionContext.CommitAsync(ct);

        return Unit.Value;
    }
}
