using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Exceptions;
using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Messaging;
using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

namespace Camilo.OrderFlow.Modules.Identity.Application.Queries.Users.GetMyPermissions;

public sealed class GetMyPermissionsQueryHandler : IRequestHandler<GetMyPermissionsQuery, IReadOnlyList<string>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionService _permissionService;

    public GetMyPermissionsQueryHandler(ICurrentUser currentUser, IPermissionService permissionService)
    {
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<string>> Handle(GetMyPermissionsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var perms = await _permissionService.GetPermissionsAsync(userId, ct);
        return perms.OrderBy(p => p).ToList();
    }
}
