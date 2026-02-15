using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
