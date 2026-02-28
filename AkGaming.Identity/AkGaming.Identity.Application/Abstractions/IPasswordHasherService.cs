using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Abstractions;

public interface IPasswordHasherService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password, string passwordHash);
}
