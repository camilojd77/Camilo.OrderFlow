using Microsoft.AspNetCore.Authorization;

namespace Company.Product.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }

    public PermissionRequirement(string code)
    {
        Code = code;
    }
}
