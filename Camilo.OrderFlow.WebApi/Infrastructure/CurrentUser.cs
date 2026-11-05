using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using System.Security.Claims;

namespace Camilo.OrderFlow.WebApi.Infrastructure;

internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated is true;
}
