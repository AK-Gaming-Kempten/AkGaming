using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.UnitTests.Fakes;

internal sealed class JwtTokenServiceStub : IJwtTokenService
{
    public AccessTokenResult GenerateAccessToken(User user)
    {
        return new AccessTokenResult($"access::{user.Id}", DateTime.UtcNow.AddMinutes(15));
    }
}
