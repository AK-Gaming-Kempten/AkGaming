using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api.Endpoints;

public static class MemberDeletionEndpoints {
    public static IEndpointRouteBuilder MapMemberDeletionEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands")
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{memberId}", async (Guid memberId, IMemberDeletionService service) => {
            var result = await service.DeleteMemberAsync(memberId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}