using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

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
        }).RequireAuthorization("management.members.status.manage");

        group.MapPut("/{memberId:guid}/insertStatusChangeEvent", async (
            [FromRoute] Guid memberId,
            [FromBody] MembershipStatusChangeEventDto changeEvent,
            [FromServices] IMembershipUpdateService service
        ) => {
            var result = await service.InsertMembershipStatusChangeEventAsync(memberId, changeEvent);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        }).RequireAuthorization("management.members.status.manage");

        // ----- Admin OR member owner -----
        group.MapGet("/{memberId:guid}/endOfTrial", async (
            [FromRoute] Guid memberId,
            ClaimsPrincipal user,
            [FromServices] IMembershipUpdateService service,
            [FromServices] IMemberQueryService memberQueryService
        ) => {
            if (!user.HasClaim("permission", "management.members.read")) {
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
            if (!user.HasClaim("permission", "management.members.read")) {
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
