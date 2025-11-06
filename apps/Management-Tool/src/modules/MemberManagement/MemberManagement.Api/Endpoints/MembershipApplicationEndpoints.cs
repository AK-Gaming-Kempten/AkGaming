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

        group.MapPost("/{userId}/applyForMembership", async ([FromRoute] Guid userId,[FromBody] MemberCreationDto memberDto, [FromServices] IMembershipApplicationService service) => {
            var result = await service.ApplyForMembershipAsync(userId, memberDto);
            return result.IsSuccess ? Results.Created($"/members/{result.Value}",result.Value) : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}