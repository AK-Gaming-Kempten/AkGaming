using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AkGaming.Tournaments.Tests.WebApi;

internal static class WebApiControllerTestHelpers
{
    public static void AssertOkValue<T>(ActionResult<T> response, T expectedValue)
    {
        var ok = response.Result as OkObjectResult;

        Assert.Multiple(() =>
        {
            Assert.That(ok, Is.Not.Null);
            Assert.That(ok!.Value, Is.SameAs((object?)expectedValue));
        });
    }

    public static void SetAuthenticatedUser(ControllerBase controller, string userId = "captain-1", params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    public static TeamDto Team(Guid id)
    {
        return new TeamDto(id, "lol", "AKG Blue", null, null, null, null, [], []);
    }

    public static PlayerProfileDto UserProfile(
        Guid? id = null,
        string name = "Summoner")
    {
        return new PlayerProfileDto(
            id ?? Guid.NewGuid(),
            "lol",
            null,
            PlayerProfileTypeDto.User,
            name,
            null,
            "user-1",
            null,
            null,
            DateTimeOffset.UtcNow);
    }

    public static PlayerProfileDto GuestProfile(
        Guid teamId,
        Guid? id = null,
        string name = "Guest Mid")
    {
        return new PlayerProfileDto(
            id ?? Guid.NewGuid(),
            "lol",
            teamId,
            PlayerProfileTypeDto.Guest,
            name,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
    }

    public static TournamentRegistrationDto Registration(
        Guid id,
        Guid? teamId = null,
        Guid? tournamentId = null)
    {
        return new TournamentRegistrationDto(
            id,
            tournamentId ?? Guid.NewGuid(),
            teamId ?? Guid.NewGuid(),
            TournamentRegistrationStatusDto.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            false,
            []);
    }
}
