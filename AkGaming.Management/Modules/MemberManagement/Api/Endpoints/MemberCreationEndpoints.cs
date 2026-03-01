using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MemberCreationEndpoints {
    public static IEndpointRouteBuilder MapMemberCreationEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Queries")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", async (MemberCreationDto memberCreationDto, IMemberCreationService service) => {
            var result = await service.CreateMemberAsync(memberCreationDto);
            return result.IsSuccess ? Results.Created($"/members/{result.Value}",result.Value) : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}