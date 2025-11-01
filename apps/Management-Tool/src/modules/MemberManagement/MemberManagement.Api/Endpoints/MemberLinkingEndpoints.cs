using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MemberLinkingEndpoints {
    public static IEndpointRouteBuilder MapMemberLinkingEndpoints(this IEndpointRouteBuilder endpoints) {
     
        var group = endpoints.MapGroup("/api/members")
            .WithTags("Members - Linking")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/linkToUser", async (Guid memberId, Guid userId, IMemberLinkingService service) => {
            var result = await service.LinkMemberToUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        group.MapPost("/unlinkFromUser", async (Guid memberId, Guid userId, IMemberLinkingService service) => {
            var result = await service.UnlinkMemberFromUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}