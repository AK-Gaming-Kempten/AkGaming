using System.Security.Cryptography;
using System.Text;
using AkGaming.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AkGaming.Identity.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public DateTime GetExpiresAtUtc()
    {
        return DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);
    }
}
