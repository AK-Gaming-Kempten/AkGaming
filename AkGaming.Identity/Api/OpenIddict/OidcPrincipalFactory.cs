using System.Security.Claims;
using AkGaming.Identity.Contracts.Auth;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AkGaming.Identity.Api.OpenIddict;

internal static class OidcPrincipalFactory
{
    internal static ClaimsPrincipal Create(CurrentUserResponse user, IEnumerable<string> scopes)
    {
        var scopeSet = scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.UserId.ToString());
        identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email);
        identity.SetClaim(OpenIddictConstants.Claims.Name, user.Username);
        identity.SetClaim(OpenIddictConstants.Claims.PreferredUsername, user.Username);
        identity.SetClaim(OpenIddictConstants.Claims.Username, user.Username);
        identity.AddClaim(new Claim( OpenIddictConstants.Claims.EmailVerified, user.IsEmailVerified ? "true" : "false", ClaimValueTypes.Boolean));

        foreach (var role in user.Roles)
        {
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, role));
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopeSet);

        var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (scopeSet.Contains("management_api"))
        {
            resources.Add("management_api");
        }

        if (resources.Count > 0)
        {
            principal.SetResources(resources);
        }

        principal.SetDestinations(claim => GetDestinations(claim, scopeSet));
        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim, IReadOnlySet<string> scopes)
    {
        yield return OpenIddictConstants.Destinations.AccessToken;

        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.IdentityToken;
                break;

            case OpenIddictConstants.Claims.Email:
            case OpenIddictConstants.Claims.EmailVerified:
                if (scopes.Contains(OpenIddictConstants.Scopes.Email))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                break;

            case OpenIddictConstants.Claims.Name:
            case OpenIddictConstants.Claims.PreferredUsername:
            case OpenIddictConstants.Claims.Username:
                if (scopes.Contains(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                break;

            case OpenIddictConstants.Claims.Role:
                if (scopes.Contains(OpenIddictConstants.Scopes.Roles))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                break;
        }
    }
}
