using System.Security.Claims;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Constants;

namespace AkGaming.Identity.Api.Endpoints;

internal static class AdminEndpoints
{
    internal static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin").RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
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
        });

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
        });

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
        });

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
        });

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
        });

        admin.MapGet("/roles", async (IAuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.GetRolesAsync(cancellationToken);
            return Results.Ok(response);
        });

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
        });

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
        });

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
        });

        return app;
    }
}
