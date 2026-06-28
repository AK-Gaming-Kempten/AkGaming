using System.Security.Claims;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Constants;
using OpenIddict.Validation.AspNetCore;

namespace AkGaming.Identity.Api.Endpoints;

internal static class AdminEndpoints
{
    internal static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin").RequireAuthorization("ManagementApiAccess");
        admin.RequireRateLimiting("auth");

        admin.MapGet("/users", async (int page, int pageSize, string? search, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.GetUsersAsync(page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize, search, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityUsersRead);

        admin.MapGet("/audit-logs", async (int page, int pageSize, string? search, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.GetAuditLogsAsync(page <= 0 ? 1 : page, pageSize <= 0 ? 25 : pageSize, search, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityAuditRead);

        admin.MapGet("/users/{userId:guid}", async (Guid userId, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.GetUserDetailsAsync(userId, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityUsersRead);

        admin.MapGet("/users/{userId:guid}/roles", async (Guid userId, IAuthService authService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await authService.GetUserRolesAsync(userId, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityUsersRead);

        admin.MapPut("/users/{userId:guid}/roles", async (Guid userId, AdminSetUserRolesRequest request, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.SetUserRolesAsync(actorUserId, userId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityUsersManage);

        admin.MapGet("/roles", async (IAuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.GetRolesAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization(PermissionNames.IdentityRolesRead);

        admin.MapGet("/permissions", async (IAuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.GetPermissionsAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization(PermissionNames.IdentityRolesRead);

        admin.MapGet("/opencloud-roles", async (IAuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.GetOpenCloudRolesAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization(PermissionNames.IdentityRolesRead);

        admin.MapPost("/roles", async (AdminCreateRoleRequest request, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.CreateRoleAsync(actorUserId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityRolesManage);

        admin.MapPut("/roles/{roleId:guid}", async (Guid roleId, AdminRenameRoleRequest request, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.RenameRoleAsync(actorUserId, roleId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityRolesManage);

        admin.MapDelete("/roles/{roleId:guid}", async (Guid roleId, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                await authService.DeleteRoleAsync(actorUserId, roleId, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.NoContent();
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityRolesManage);

        admin.MapPut("/roles/{roleId:guid}/permissions", async (Guid roleId, AdminSetRolePermissionsRequest request, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.SetRolePermissionsAsync(actorUserId, roleId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityRolesManage);

        admin.MapPut("/roles/{roleId:guid}/opencloud-roles", async (Guid roleId, AdminSetRoleOpenCloudRolesRequest request, ClaimsPrincipal user, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var response = await authService.SetRoleOpenCloudRolesAsync(actorUserId, roleId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityRolesManage);

        admin.MapGet("/oidc/clients", async (IOidcAdminService oidcAdminService, CancellationToken cancellationToken) =>
        {
            var response = await oidcAdminService.GetClientsAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapGet("/oidc/clients/{clientId}", async (string clientId, IOidcAdminService oidcAdminService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await oidcAdminService.GetClientAsync(clientId, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapPost("/oidc/clients", async (AdminCreateOidcClientRequest request, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                var response = await oidcAdminService.CreateClientAsync(actorUserId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapPut("/oidc/clients/{clientId}", async (string clientId, AdminUpdateOidcClientRequest request, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                var response = await oidcAdminService.UpdateClientAsync(actorUserId, clientId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapDelete("/oidc/clients/{clientId}", async (string clientId, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                await oidcAdminService.DeleteClientAsync(actorUserId, clientId, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.NoContent();
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapGet("/oidc/scopes", async (IOidcAdminService oidcAdminService, CancellationToken cancellationToken) =>
        {
            var response = await oidcAdminService.GetScopesAsync(cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapGet("/oidc/scopes/{scopeName}", async (string scopeName, IOidcAdminService oidcAdminService, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await oidcAdminService.GetScopeAsync(scopeName, cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapPost("/oidc/scopes", async (AdminCreateOidcScopeRequest request, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                var response = await oidcAdminService.CreateScopeAsync(actorUserId, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapPut("/oidc/scopes/{scopeName}", async (string scopeName, AdminUpdateOidcScopeRequest request, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                var response = await oidcAdminService.UpdateScopeAsync(actorUserId, scopeName, request, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.Ok(response);
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        admin.MapDelete("/oidc/scopes/{scopeName}", async (string scopeName, ClaimsPrincipal user, IOidcAdminService oidcAdminService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!EndpointUtilities.TryGetUserId(user, out var actorUserId))
                return Results.Unauthorized();

            try
            {
                await oidcAdminService.DeleteScopeAsync(actorUserId, scopeName, EndpointUtilities.GetIp(httpContext), cancellationToken);
                return Results.NoContent();
            }
            catch (AuthException exception)
            {
                return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
            }
        }).RequireAuthorization(PermissionNames.IdentityOidcManage);

        return app;
    }

}
