using System.Security.Claims;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MemberCreationEndpoints {
    public static IEndpointRouteBuilder MapMemberCreationEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands");

        group.MapPost("/", async (MemberCreationDto memberCreationDto, IMemberCreationService service) => {
            var result = await service.CreateMemberAsync(memberCreationDto);
            return result.IsSuccess ? Results.Created($"/members/{result.Value}",result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/profile", async (ClaimsPrincipal user, IMemberCreationService service) => {
            var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (!Guid.TryParse(claim, out var currentUserId)) {
                return Results.Forbid();
            }

            var result = await service.CreateUserProfileAsync(currentUserId);
            return result.IsSuccess
                ? Results.Created($"/members/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return endpoints;
    }
}
