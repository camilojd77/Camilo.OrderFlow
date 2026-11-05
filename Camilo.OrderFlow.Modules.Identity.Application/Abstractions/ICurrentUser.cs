namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
