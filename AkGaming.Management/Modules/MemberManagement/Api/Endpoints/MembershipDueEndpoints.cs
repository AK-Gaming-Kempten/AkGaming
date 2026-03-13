using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MembershipDueEndpoints {
    public static IEndpointRouteBuilder MapMembershipDueEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/membership-dues")
            .WithTags("Members - Dues");

        group.MapPost("/payment-periods", async (
            [FromBody] MembershipPaymentPeriodCreateDto request,
            ClaimsPrincipal user,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.CreatePaymentPeriodAsync(request, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods/current", async (
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetCurrentPaymentPeriodDuesAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods/{paymentPeriodId:int}", async (
            [FromRoute] int paymentPeriodId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetPaymentPeriodDuesAsync(paymentPeriodId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/payment-periods/{paymentPeriodId:int}/members", async (
            [FromRoute] int paymentPeriodId,
            [FromBody] ICollection<Guid> memberIds,
            ClaimsPrincipal user,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.AddMembersToPaymentPeriodAsync(paymentPeriodId, memberIds, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/members/{memberId:guid}", async (
            [FromRoute] Guid memberId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetDuesForMemberAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/me", async (
            ClaimsPrincipal user,
            [FromServices] IMemberQueryService memberQueryService,
            [FromServices] IMembershipDueService service
        ) => {
            var currentUserId = GetCurrentUserIdOrNull(user);
            if (currentUserId is null)
                return Results.Forbid();

            var memberResult = await memberQueryService.GetMemberByUserGuidAsync(currentUserId.Value);
            if (!memberResult.IsSuccess)
                return Results.NotFound(memberResult.Error);

            var result = await service.GetDuesForMemberAsync(memberResult.Value!.Id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPut("/{dueId:int}", async (
            [FromRoute] int dueId,
            [FromBody] MembershipDueDto due,
            ClaimsPrincipal user,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.UpdateDueAsync(dueId, due, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }
}
