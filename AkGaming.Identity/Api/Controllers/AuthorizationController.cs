using System.Security.Claims;
using AkGaming.Identity.Api.Authentication;
using AkGaming.Identity.Api.Endpoints;
using AkGaming.Identity.Api.OpenIddict;
using AkGaming.Identity.Application.Abstractions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Immutable;

namespace AkGaming.Identity.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AuthorizationController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAuthHardeningSettings _hardeningSettings;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;

    public AuthorizationController(
        IAuthService authService,
        IAuthHardeningSettings hardeningSettings,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager)
    {
        _authService = authService;
        _hardeningSettings = hardeningSettings;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
    }

    [AcceptVerbs("GET", "POST")]
    [AllowAnonymous]
    [Route("~/connect/authorize")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request cannot be resolved.");
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.InvalidClient, "The client application could not be resolved."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken);
        if (application is null)
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.InvalidClient, "The client application is not registered."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var applicationId = await _applicationManager.GetIdAsync(application, cancellationToken)
            ?? throw new InvalidOperationException("The client application identifier could not be resolved.");
        var clientId = await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? request.ClientId;
        var displayName = await _applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? clientId;
        var consentType = await _applicationManager.GetConsentTypeAsync(application, cancellationToken);

        var authenticationResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticationResult.Succeeded)
        {
            if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
            {
                return Forbid(
                    BuildOpenIddictError(OpenIddictConstants.Errors.LoginRequired, "The user is not logged in."),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (!EndpointUtilities.TryGetUserId(authenticationResult.Principal!, out var userId))
        {
            await LocalSessionManager.SignOutAsync(HttpContext);
            return Redirect($"/account/login?returnUrl={Uri.EscapeDataString(Request.PathBase + Request.Path + Request.QueryString)}");
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (_hardeningSettings.RequireVerifiedEmailForLogin && !user.IsEmailVerified)
        {
            if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
            {
                return Forbid(
                    BuildOpenIddictError(OpenIddictConstants.Errors.LoginRequired, "Email verification is required before continuing."),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            return Redirect(LocalSessionManager.BuildVerificationRedirect(HttpContext, returnUrl, "Verify your email before continuing."));
        }

        var scopes = request.GetScopes().ToImmutableArray();
        var existingAuthorizations = await FindPermanentAuthorizationsAsync(user.UserId.ToString(), applicationId, scopes, cancellationToken);
        var consentDecision = ReadConsentDecision();

        if (request.HasPromptValue(OpenIddictConstants.PromptValues.Consent) && !string.Equals(consentDecision, "accept", StringComparison.Ordinal))
        {
            return Redirect(BuildConsentRedirect(displayName));
        }

        switch (consentType)
        {
            case OpenIddictConstants.ConsentTypes.Explicit:
                if (string.Equals(consentDecision, "deny", StringComparison.Ordinal))
                {
                    return Forbid(
                        BuildOpenIddictError(OpenIddictConstants.Errors.AccessDenied, "The authorization request was denied."),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                if (existingAuthorizations.Count == 0 && request.HasPromptValue(OpenIddictConstants.PromptValues.None))
                {
                    return Forbid(
                        BuildOpenIddictError(OpenIddictConstants.Errors.ConsentRequired, "Interactive consent is required for this client."),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                if (existingAuthorizations.Count == 0 && !string.Equals(consentDecision, "accept", StringComparison.Ordinal))
                {
                    return Redirect(BuildConsentRedirect(displayName));
                }
                break;

            case OpenIddictConstants.ConsentTypes.External:
                if (existingAuthorizations.Count == 0)
                {
                    return Forbid(
                        BuildOpenIddictError(OpenIddictConstants.Errors.ConsentRequired, "This client requires administrator-approved consent."),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
                break;

            case OpenIddictConstants.ConsentTypes.Systematic:
                if (string.Equals(consentDecision, "deny", StringComparison.Ordinal))
                {
                    return Forbid(
                        BuildOpenIddictError(OpenIddictConstants.Errors.AccessDenied, "The authorization request was denied."),
                        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                if (!string.Equals(consentDecision, "accept", StringComparison.Ordinal))
                {
                    if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
                    {
                        return Forbid(
                            BuildOpenIddictError(OpenIddictConstants.Errors.ConsentRequired, "Interactive consent is required for this client."),
                            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    }

                    return Redirect(BuildConsentRedirect(displayName));
                }
                break;
        }

        var principal = OidcPrincipalFactory.Create(user, scopes);
        principal.SetPresenters(clientId);

        if (existingAuthorizations.Count > 0)
        {
            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(existingAuthorizations[0], cancellationToken));
        }
        else
        {
            var authorization = await _authorizationManager.CreateAsync(
                principal,
                user.UserId.ToString(),
                applicationId,
                OpenIddictConstants.AuthorizationTypes.Permanent,
                scopes,
                cancellationToken);
            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization, cancellationToken));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        string BuildConsentRedirect(string clientDisplayName)
        {
            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            var redirect = QueryHelpers.AddQueryString("/account/consent", "returnUrl", returnUrl);
            redirect = QueryHelpers.AddQueryString(redirect, "clientId", clientId);
            redirect = QueryHelpers.AddQueryString(redirect, "clientName", clientDisplayName);
            redirect = QueryHelpers.AddQueryString(redirect, "scopes", string.Join(" ", scopes));
            return redirect;
        }
    }

    [HttpPost("~/connect/token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request cannot be resolved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.UnsupportedGrantType, "The specified grant type is not supported."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var authenticationResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null)
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.InvalidGrant, "The token request is no longer valid."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!EndpointUtilities.TryGetUserId(authenticationResult.Principal, out var userId))
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.InvalidGrant, "The token subject is invalid."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (_hardeningSettings.RequireVerifiedEmailForLogin && !user.IsEmailVerified)
        {
            return Forbid(
                BuildOpenIddictError(OpenIddictConstants.Errors.InvalidGrant, "Email verification is required before accessing services."),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = OidcPrincipalFactory.Create(user, authenticationResult.Principal.GetScopes());

        var resources = authenticationResult.Principal.GetResources();
        if (resources.Any())
        {
            principal.SetResources(resources);
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [AcceptVerbs("GET", "POST")]
    [Route("~/connect/userinfo")]
    public async Task<IActionResult> UserInfo(CancellationToken cancellationToken)
    {
        var authenticationResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!authenticationResult.Succeeded || authenticationResult.Principal is null)
        {
            return Challenge();
        }

        if (!EndpointUtilities.TryGetUserId(authenticationResult.Principal, out var userId))
        {
            return Challenge();
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(new
        {
            sub = user.UserId,
            name = user.Email,
            preferred_username = user.Username,
            username = user.Username,
            email = user.Email,
            email_verified = user.IsEmailVerified,
            role = user.Roles
        });
    }

    [AcceptVerbs("GET", "POST")]
    [AllowAnonymous]
    [Route("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        await LocalSessionManager.SignOutAsync(HttpContext);

        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = string.IsNullOrWhiteSpace(request?.PostLogoutRedirectUri)
                    ? "/account/login"
                    : request.PostLogoutRedirectUri
            },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static AuthenticationProperties BuildOpenIddictError(string error, string description)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });
    }

    private async Task<List<object>> FindPermanentAuthorizationsAsync(
        string subject,
        string clientId,
        ImmutableArray<string> scopes,
        CancellationToken cancellationToken)
    {
        var authorizations = new List<object>();
        await foreach (var authorization in _authorizationManager.FindAsync(
                           subject,
                           clientId,
                           OpenIddictConstants.Statuses.Valid,
                           OpenIddictConstants.AuthorizationTypes.Permanent,
                           scopes,
                           cancellationToken))
        {
            authorizations.Add(authorization);
        }

        return authorizations;
    }

    private string? ReadConsentDecision()
    {
        if (Request.HasFormContentType && Request.Form.TryGetValue("consent", out var formValue))
        {
            return formValue.FirstOrDefault();
        }

        return Request.Query["consent"].FirstOrDefault();
    }
}
