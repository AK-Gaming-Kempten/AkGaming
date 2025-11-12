using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MemberLinkingEndpoints {
    public static IEndpointRouteBuilder MapMemberLinkingEndpoints(this IEndpointRouteBuilder endpoints) {
     
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Linking")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/{memberId}/linkToUser", async ([FromRoute] Guid memberId, Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.LinkMemberToUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        group.MapPost("/{memberId}/unlinkFromUser", async ([FromRoute] Guid memberId, Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.UnlinkMemberFromUserAsync(memberId, userId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });
        
        group.MapPost("/memberLinkingRequests", async ([FromBody] MemberLinkingRequestDto request, [FromServices] IMemberLinkingService service) => {
            var result = await service.CreateMemberLinkingRequestAsync(request);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });
        
        group.MapGet("/memberLinkingRequests", async ([FromServices] IMemberLinkingService service) => {
            var result = await service.GetAllMemberLinkingRequestsAsync();
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        group.MapGet("/{userId}/memberLinkingRequests", async ([FromRoute] Guid userId, [FromServices] IMemberLinkingService service) => {
            var result = await service.GetMemberLinkingRequestsFromUserAsync(userId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        group.MapPost("/memberLinkingRequests/{requestId}", async ([FromRoute] Guid requestId, [FromServices] IMemberLinkingService service) => {
            var result = await service.MarkMemberLinkingRequestResolvedAsync(requestId);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}