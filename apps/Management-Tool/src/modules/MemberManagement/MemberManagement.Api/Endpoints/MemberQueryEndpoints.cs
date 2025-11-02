using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Api.Endpoints;

public static class MemberQueryEndpoints {
    public static IEndpointRouteBuilder MapMemberQueryEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Queries")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IMemberQueryService service) => {
            var result = await service.GetAllMembersAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/{memberId:guid}", async (Guid memberId, IMemberQueryService service) => {
            var result = await service.GetMemberByGuidAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
        
        group.MapGet("/user/{memberId:guid}", async (Guid memberId, IMemberQueryService service) => {
            var result = await service.GetMemberByUserGuidAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
        
        group.MapGet("/byStatus/{status}", async (ContractEnums.MembershipStatus status, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(new[] { status });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
        
        group.MapGet("/byStatus", async (ContractEnums.MembershipStatus[] statuses, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(statuses);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return endpoints;
    }
}