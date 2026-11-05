using Microsoft.AspNetCore.Authorization;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Authorization;

internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }

    public PermissionRequirement(string code)
    {
        Code = code;
    }
}
