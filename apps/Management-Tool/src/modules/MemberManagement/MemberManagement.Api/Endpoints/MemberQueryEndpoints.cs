using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Api.Endpoints;

public static class MemberQueryEndpoints {
    public static IEndpointRouteBuilder MapMemberQueryEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/api/members")
            .WithTags("Members - Queries")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (IMemberQueryService service) => {
            var result = await service.GetAllMembersAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMemberQueryService service) => {
            var result = await service.GetMemberByGuidAsync(id);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
        
        group.MapGet("/status/{status}", async (ContractEnums.MembershipStatus status, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(new[] { status });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
        
        group.MapGet("/status", async (ContractEnums.MembershipStatus[] statuses, IMemberQueryService service) => {
            var result = await service.GetMembersWithStatusAsync(statuses);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return endpoints;
    }
}