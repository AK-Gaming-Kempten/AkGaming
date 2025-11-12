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
            .WithTags("Members - Commands")
            .RequireAuthorization("UserOnly");

        group.MapPost("/applyForMembership", async ([FromBody] MembershipApplicationRequestDto request, [FromServices] IMembershipApplicationService service) => {
            var result = await service.ApplyForMembershipAsync(request);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });
        
        group.MapGet("/{userId}/membershipApplicationRequests", async ([FromRoute] Guid userId, [FromServices] IMembershipApplicationService service) => {
            var result = await service.GetAllRequestFromUserAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapGet("/membershipApplicationRequests", async ([FromServices] IMembershipApplicationService service) => {
            var result = await service.GetAllRequestAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}