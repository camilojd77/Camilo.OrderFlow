using Microsoft.AspNetCore.Authorization;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Authorization;

public sealed class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public AuthorizePermissionAttribute(string code)
    {
        Policy = $"permission:{code}";
    }
}
