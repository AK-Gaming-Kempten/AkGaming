namespace AkGaming.Identity.Application.Abstractions;

public interface IRefreshTokenService
{
    string GenerateToken();
    string HashToken(string token);
    DateTime GetExpiresAtUtc();
}
