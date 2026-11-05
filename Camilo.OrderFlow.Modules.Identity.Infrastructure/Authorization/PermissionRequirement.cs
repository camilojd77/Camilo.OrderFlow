using Microsoft.AspNetCore.Authorization;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }

    public PermissionRequirement(string code)
    {
        Code = code;
    }
}
