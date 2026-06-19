using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class PaymentInformationEndpoints {
    public static IEndpointRouteBuilder MapPaymentInformationEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/payment-information")
            .WithTags("Payment Information")
            .RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, IPaymentInformationService service) => {
            if (!TryGetUserId(user, out var userId)) return Results.Forbid();
            var result = await service.GetForUserAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (PaymentInformationDto request, ClaimsPrincipal user, IPaymentInformationService service) => {
            if (!TryGetUserId(user, out var userId)) return Results.Forbid();
            var result = await service.CreateAsync(userId, request);
            return result.IsSuccess ? Results.Created($"/payment-information/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPut("/{id:guid}", async (Guid id, PaymentInformationDto request, ClaimsPrincipal user, IPaymentInformationService service) => {
            if (!TryGetUserId(user, out var userId)) return Results.Forbid();
            var result = await service.UpdateAsync(userId, id, request);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, IPaymentInformationService service) => {
            if (!TryGetUserId(user, out var userId)) return Results.Forbid();
            var result = await service.DeleteAsync(userId, id);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }
}
