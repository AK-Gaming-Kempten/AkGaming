using System.Security.Claims;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MembershipApplicationEndpoints {
    public static IEndpointRouteBuilder MapMembershipApplicationEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands");
        
        group.MapPost("/applyForMembership", async (
            [FromBody] MembershipApplicationRequestDto request,
            ClaimsPrincipal user,
            [FromServices] IMembershipApplicationService service
        ) => {
            if (!user.IsInRole("Admin")) {
                var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
                if (!Guid.TryParse(claim, out var currentUserId)) return Results.Forbid();
                if (request.IssuingUserId != currentUserId) return Results.Forbid();
            }

            var result = await service.ApplyForMembershipAsync(request, GetCurrentUserIdOrNull(user));
            return result.IsSuccess
                ? Results.Created($"/members/{request.IssuingUserId}/membershipApplicationRequests", null)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization(); 
        
        group.MapGet("/{userId:guid}/membershipApplicationRequests", async (
            [FromRoute] Guid userId,
            [FromServices] IMembershipApplicationService service
        ) => {
            var result = await service.GetAllRequestFromUserAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOrSelfRouteUserId");
        
        group.MapGet("/membershipApplicationRequests", async ([FromServices] IMembershipApplicationService service) => {
            var result = await service.GetAllRequestAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");
        
        group.MapPost("/membershipApplicationRequests/{requestId:guid}/accept", async ([FromRoute] Guid requestId, ClaimsPrincipal user, [FromServices] IMembershipApplicationService service) => {
            var result = await service.AcceptMembershipApplicationAsync(requestId, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Created($"/members/{requestId}/membershipApplicationRequests", null) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/membershipApplicationRequests/{requestId:guid}/reject", async ([FromRoute] Guid requestId, [FromServices] IMembershipApplicationService service) => {
            var result = await service.RejectMembershipApplicationAsync(requestId);
            return result.IsSuccess ? Results.Created($"/members/{requestId}/membershipApplicationRequests", null) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }
}
