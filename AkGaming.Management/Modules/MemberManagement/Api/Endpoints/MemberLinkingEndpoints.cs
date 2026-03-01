using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MemberLinkingEndpoints {
    public static IEndpointRouteBuilder MapMemberLinkingEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Linking");

        // ----- Admin-only operations -----
        group.MapPost("/{memberId:guid}/linkToUser", async ([FromRoute] Guid memberId, [FromBody] Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.LinkMemberToUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/{memberId:guid}/unlinkFromUser", async ([FromRoute] Guid memberId, [FromBody] Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.UnlinkMemberFromUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/memberLinkingRequests/{requestId:guid}/markResolved", async ([FromRoute] Guid requestId, ClaimsPrincipal user, [FromServices] IMemberLinkingService service) => {
            var result = await service.MarkMemberLinkingRequestResolvedAsync(requestId, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/memberLinkingRequests/{requestId:guid}/accept", async ([FromRoute] Guid requestId, ClaimsPrincipal user, [FromServices] IMemberLinkingService service) => {
            var result = await service.AcceptMemberLinkingRequestAsync(requestId, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/memberLinkingRequests/{requestId:guid}/reject", async ([FromRoute] Guid requestId, ClaimsPrincipal user, [FromServices] IMemberLinkingService service) => {
            var result = await service.RejectMemberLinkingRequestAsync(requestId, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        // ----- User-scoped + admin endpoints -----
        
        group.MapGet("/memberLinkingRequests", async ([FromServices] IMemberLinkingService service) => {
            var result = await service.GetAllMemberLinkingRequestsAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");
        
        group.MapGet("/{userId:guid}/memberLinkingRequests", async ([FromRoute] Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.GetMemberLinkingRequestsFromUserAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOrSelfRouteUserId");


        group.MapPost("/memberLinkingRequests", async (
            [FromBody] MemberLinkingRequestDto request,
            ClaimsPrincipal user,
            [FromServices] IMemberLinkingService service
        ) => {
            // Allow admins outright
            if (!user.IsInRole("Admin")) {
                var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
                if (!Guid.TryParse(claim, out var currentUserId)) return Results.Forbid();
                if (request.IssuingUserId != currentUserId) return Results.Forbid();
            }

            var result = await service.CreateMemberLinkingRequestAsync(request, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Created($"/members/{request.IssuingUserId}/memberLinkingRequests", null) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }
}
