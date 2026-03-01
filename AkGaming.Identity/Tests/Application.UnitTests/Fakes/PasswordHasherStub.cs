using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class PasswordHasherStub : IPasswordHasherService
{
    public string HashPassword(User user, string password) => $"hash::{password}";

    public bool VerifyPassword(User user, string password, string passwordHash)
    {
        return passwordHash == HashPassword(user, password);
    }
}
