using System.Security.Claims;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;

namespace AkGaming.Identity.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth");
        auth.RequireRateLimiting("auth");

        auth.MapPost("/register", async (RegisterRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.RegisterAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.LoginAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/refresh", async (RefreshRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.RefreshAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/logout", async (LogoutRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            await authService.LogoutAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
            return Results.NoContent();
        });

        auth.MapGet("/logout", async (string? returnUrl, string? refreshToken, IAuthService authService, IConfiguration configuration, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await authService.LogoutAsync(new LogoutRequest(refreshToken), EndpointUtilities.GetIp(httpContext), cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return Results.Redirect("/ui/login.html");
            }

            if (returnUrl.StartsWith("/", StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal))
            {
                return Results.Redirect(returnUrl);
            }

            var allowed = EndpointUtilities.IsAllowedRedirectUri(
                returnUrl,
                configuration,
                out _,
                out _);

            if (!allowed)
            {
                return Results.Problem(statusCode: 400, detail: "returnUrl is not allowed.");
            }

            return Results.Redirect(returnUrl);
        });

        auth.MapPost("/redirect/finalize", (RedirectFinalizeRequest request, IConfiguration configuration, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("RedirectFinalize");

            if (string.IsNullOrWhiteSpace(request.RedirectUri))
            {
                logger.LogWarning("Redirect finalize rejected: missing redirectUri.");
                return Results.Problem(statusCode: 400, detail: "redirectUri is required.");
            }

            var allowed = EndpointUtilities.IsAllowedRedirectUri(
                request.RedirectUri,
                configuration,
                out var reason,
                out var evaluations);

            logger.LogInformation(
                "Redirect finalize check: redirectUri={RedirectUri}, allowed={Allowed}, reason={Reason}, evaluations={Evaluations}",
                request.RedirectUri,
                allowed,
                reason,
                string.Join(" | ", evaluations));

            if (!allowed)
            {
                return Results.Problem(statusCode: 400, detail: "redirectUri is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                logger.LogWarning("Redirect finalize rejected: token payload missing for redirectUri={RedirectUri}.", request.RedirectUri);
                return Results.Problem(statusCode: 400, detail: "accessToken and refreshToken are required.");
            }

            var redirectUrl = EndpointUtilities.BuildExternalRedirectUrl(request);
            logger.LogInformation("Redirect finalize accepted: redirectUri={RedirectUri}, redirectUrl={RedirectUrl}.", request.RedirectUri, redirectUrl);
            return Results.Ok(new { redirectUrl });
        });

        auth.MapGet("/me", [Authorize] async (ClaimsPrincipal user, IAuthService authService, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.GetCurrentUserAsync(userId, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/email/send-verification", async (EmailVerificationRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.RequestEmailVerificationAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/email/send-verification/me", [Authorize] async (ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.RequestEmailVerificationForUserAsync(userId, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapPost("/email/verify", async (VerifyEmailRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                await authService.VerifyEmailAsync(request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.NoContent();
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        });

        auth.MapGet("/email/verify-link", async (string token, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                await authService.VerifyEmailAsync(new VerifyEmailRequest(token), EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Redirect("/ui/index.html?emailVerified=1");
            }
            catch (AuthException exception)
            {
                var message = Uri.EscapeDataString(exception.Message);
                return Results.Redirect($"/ui/index.html?emailVerified=0&message={message}");
            }
        });

        auth.MapGet("/discord/start", async (HttpContext httpContext, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var redirectUri = httpContext.Request.Query["redirect_uri"].ToString();
            var state = httpContext.Request.Query["state"].ToString();
            var response = await authService.GetDiscordStartUrlAsync(
                string.IsNullOrWhiteSpace(redirectUri) ? null : redirectUri,
                string.IsNullOrWhiteSpace(state) ? null : state,
                cancellationToken);
            return Results.Redirect(response.AuthorizationUrl);
        });

        auth.MapGet("/discord/callback", async (string code, string state, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.HandleDiscordCallbackAsync(code, state, EndpointUtilities.GetIp(httpContext), cancellationToken);
                var fragment = EndpointUtilities.BuildDiscordCallbackFragment(
                    success: true,
                    message: response.Message,
                    accessToken: response.Tokens?.AccessToken,
                    refreshToken: response.Tokens?.RefreshToken,
                    accessTokenExpiresAtUtc: response.Tokens?.AccessTokenExpiresAtUtc,
                    redirectUri: response.RedirectUri,
                    state: response.State,
                    linked: response.Linked,
                    createdUser: response.CreatedUser);

                return Results.Redirect($"/ui/callback.html#{fragment}");
            }
            catch (AuthException exception)
            {
                var fragment = EndpointUtilities.BuildDiscordCallbackFragment(
                    success: false,
                    message: exception.Message,
                    errorCode: exception.StatusCode.ToString());

                return Results.Redirect($"/ui/callback.html#{fragment}");
            }
        });

        auth.MapPost("/discord/link", [Authorize] async (ClaimsPrincipal user, IAuthService authService, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var userId))
            {
                return Results.Unauthorized();
            }

            var response = await authService.GetDiscordLinkUrlAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }
}
