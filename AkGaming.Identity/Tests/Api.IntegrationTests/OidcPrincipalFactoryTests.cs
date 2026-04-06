using AkGaming.Identity.Api.OpenIddict;
using AkGaming.Identity.Contracts.Auth;
using OpenIddict.Abstractions;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class OidcPrincipalFactoryTests
{
    [Fact]
    public void Create_WithOpenIdEmailProfileAndRoles_SetsClaimDestinations()
    {
        var user = new CurrentUserResponse(
            Guid.NewGuid(),
            "principal@example.com",
            "Principal User",
            true,
            ["User", "Admin"],
            null);

        var principal = OidcPrincipalFactory.Create(user, ["openid", "profile", "email", "roles", "management_api"]);

        Assert.Contains("management_api", principal.GetResources());

        var subClaim = principal.FindFirst(OpenIddictConstants.Claims.Subject);
        Assert.NotNull(subClaim);
        Assert.Contains(OpenIddictConstants.Destinations.AccessToken, subClaim!.GetDestinations());
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, subClaim.GetDestinations());

        var emailClaim = principal.FindFirst(OpenIddictConstants.Claims.Email);
        Assert.NotNull(emailClaim);
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, emailClaim!.GetDestinations());

        var nameClaim = principal.FindFirst(OpenIddictConstants.Claims.Name);
        Assert.NotNull(nameClaim);
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, nameClaim!.GetDestinations());

        var preferredUsernameClaim = principal.FindFirst("preferred_username");
        Assert.NotNull(preferredUsernameClaim);
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, preferredUsernameClaim!.GetDestinations());

        var usernameClaim = principal.FindFirst("username");
        Assert.NotNull(usernameClaim);
        Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, usernameClaim!.GetDestinations());

        var roleClaims = principal.FindAll(OpenIddictConstants.Claims.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.All(roleClaims, claim => Assert.Contains(OpenIddictConstants.Destinations.IdentityToken, claim.GetDestinations()));
    }
}
