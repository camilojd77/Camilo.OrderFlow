using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string PermissionPrefix = "permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var code = policyName[PermissionPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(code))
                .Build();

            return policy;
        }

        return await base.GetPolicyAsync(policyName);
    }
}
