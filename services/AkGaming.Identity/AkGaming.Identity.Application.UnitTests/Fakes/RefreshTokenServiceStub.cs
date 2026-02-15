using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class RefreshTokenServiceStub : IRefreshTokenService
{
    private int _sequence;

    public string GenerateToken()
    {
        _sequence++;
        return $"refresh-{_sequence}";
    }

    public string HashToken(string token)
    {
        return $"hash::{token}";
    }

    public DateTime GetExpiresAtUtc()
    {
        return DateTime.UtcNow.AddDays(7);
    }
}
