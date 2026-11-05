using Camilo.OrderFlow.Modules.Identity.Application.Abstractions;
using BC = BCrypt.Net.BCrypt;

namespace Camilo.OrderFlow.Modules.Identity.Infrastructure.Auth;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BC.HashPassword(password);

    public bool Verify(string password, string hash) => BC.Verify(password, hash);
}
