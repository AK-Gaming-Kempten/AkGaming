using System.Security.Claims;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MembershipUpdateEndpoints {
    public static IEndpointRouteBuilder MapMembershipUpdateEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands");

        // ----- ADMIN-ONLY operations -----
        group.MapPut("/{memberId:guid}/updateStatus", async (
            [FromRoute] Guid memberId,
            [FromBody] MembershipStatus status,
            [FromServices] IMembershipUpdateService service
        ) => {
            var result = await service.UpdateMembershipStatusAsync(memberId, status);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{memberId:guid}/insertStatusChangeEvent", async (
            [FromRoute] Guid memberId,
            [FromBody] MembershipStatusChangeEventDto changeEvent,
            [FromServices] IMembershipUpdateService service
        ) => {
            var result = await service.InsertMembershipStatusChangeEventAsync(memberId, changeEvent);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        // ----- Admin OR member owner -----
        group.MapGet("/{memberId:guid}/endOfTrial", async (
            [FromRoute] Guid memberId,
            ClaimsPrincipal user,
            [FromServices] IMembershipUpdateService service,
            [FromServices] IMemberQueryService memberQueryService
        ) => {
            // Admins always allowed
            if (!user.IsInRole("Admin")) {
                // find the userId owning this member
                var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
                if (!Guid.TryParse(claim, out var currentUserId)) return Results.Forbid();

                var memberResult = await memberQueryService.GetMemberByGuidAsync(memberId);
                if (!memberResult.IsSuccess || memberResult.Value == null) return Results.NotFound();

                // check ownership
                if (memberResult.Value.UserId != currentUserId) return Results.Forbid();
            }

            var result = await service.GetDefaultEndOfTrialPeriodAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization(); 

        group.MapGet("/{memberId:guid}/statusChanges", async (
            [FromRoute] Guid memberId,
            ClaimsPrincipal user,
            [FromServices] IMembershipUpdateService service,
            [FromServices] IMemberQueryService memberQueryService
        ) => {
            if (!user.IsInRole("Admin")) {
                var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
                if (!Guid.TryParse(claim, out var currentUserId)) return Results.Forbid();

                var memberResult = await memberQueryService.GetMemberByGuidAsync(memberId);
                if (!memberResult.IsSuccess || memberResult.Value == null) return Results.NotFound();

                if (memberResult.Value.UserId != currentUserId) return Results.Forbid();
            }

            var result = await service.GetMembershipStatusChangesAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return endpoints;
    }
}
