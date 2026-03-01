using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MemberQueryEndpoints {
    public static IEndpointRouteBuilder MapMemberQueryEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Queries");

        // ----- Admin-only (broad data) -----

        group.MapGet("/", async (IMemberQueryService service) => {
            var result = await service.GetAllMembersAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/{memberId:guid}", async (Guid memberId, IMemberQueryService service) => {
            var result = await service.GetMemberByGuidAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/byStatus/{status}", async (ContractEnums.MembershipStatus status, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(new[] { status });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/byStatus", async (ContractEnums.MembershipStatus[] statuses, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(statuses);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization("AdminOnly");

        // ----- Admin or self (scoped to a single user) -----
        
        group.MapGet("/user/{userId:guid}", async (Guid userId, IMemberQueryService service) => {
            var result = await service.GetMemberByUserGuidAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization("AdminOrSelfRouteUserId");
        
        group.MapGet("/me", async (ClaimsPrincipal user, IMemberQueryService service) => {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (!Guid.TryParse(claim, out var currentUserId)) return Results.Forbid();

            var result = await service.GetMemberByUserGuidAsync(currentUserId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        }).RequireAuthorization(); 

        return endpoints;
    }
}
