namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
