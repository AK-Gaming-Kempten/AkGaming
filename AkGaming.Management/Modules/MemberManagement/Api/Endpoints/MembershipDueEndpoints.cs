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
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods", async (
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetPaymentPeriodsAsync();
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods/current", async (
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetCurrentPaymentPeriodDuesAsync();
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            if (IsNotFoundError(result.Error))
                return Results.NotFound(result.Error);

            return Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods/{paymentPeriodId:int}", async (
            [FromRoute] int paymentPeriodId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetPaymentPeriodDuesAsync(paymentPeriodId);
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/payment-periods/{paymentPeriodId:int}/reminder-dispatch", async (
            [FromRoute] int paymentPeriodId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetReminderDispatchPreviewForPaymentPeriodAsync(paymentPeriodId);
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/payment-periods/{paymentPeriodId:int}/members", async (
            [FromRoute] int paymentPeriodId,
            [FromBody] ICollection<Guid> memberIds,
            ClaimsPrincipal user,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.AddMembersToPaymentPeriodAsync(paymentPeriodId, memberIds, GetCurrentUserIdOrNull(user));
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/members/{memberId:guid}", async (
            [FromRoute] Guid memberId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetDuesForMemberAsync(memberId);
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/{dueId:int}/reminder-email", async (
            [FromRoute] int dueId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetReminderEmailPreviewAsync(dueId);
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/{dueId:int}/reminder-dispatch", async (
            [FromRoute] int dueId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.GetReminderDispatchPreviewForDueAsync(dueId);
            return MapResult(result);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/{dueId:int}/send-reminder", async (
            [FromRoute] int dueId,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.SendReminderEmailAsync(dueId);
            if (result.IsSuccess)
                return Results.Ok();

            if (IsNotFoundError(result.Error))
                return Results.NotFound(result.Error);

            if (IsServerError(result.Error))
                return Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError);

            return Results.BadRequest(result.Error);
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
            return MapResult(result);
        }).RequireAuthorization();

        group.MapPut("/{dueId:int}", async (
            [FromRoute] int dueId,
            [FromBody] MembershipDueDto due,
            ClaimsPrincipal user,
            [FromServices] IMembershipDueService service
        ) => {
            var result = await service.UpdateDueAsync(dueId, due, GetCurrentUserIdOrNull(user));
            if (result.IsSuccess)
                return Results.NoContent();

            if (IsNotFoundError(result.Error))
                return Results.NotFound(result.Error);

            return Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }

    private static bool IsNotFoundError(string? error) {
        if (string.IsNullOrWhiteSpace(error))
            return false;

        return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("no payment period exists", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsServerError(string? error) {
        if (string.IsNullOrWhiteSpace(error))
            return false;

        return error.Contains("database error", StringComparison.OrdinalIgnoreCase)
            || error.Contains("failed to send reminder email", StringComparison.OrdinalIgnoreCase);
    }

    private static IResult MapResult<T>(AkGaming.Core.Common.Generics.Result<T> result) {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        if (IsNotFoundError(result.Error))
            return Results.NotFound(result.Error);

        if (IsServerError(result.Error))
            return Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError);

        return Results.BadRequest(result.Error);
    }
}
