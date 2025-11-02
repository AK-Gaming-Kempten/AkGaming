using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MemberUpdateEndpoints {
    public static IEndpointRouteBuilder MapMemberUpdateEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{memberId}", async (Guid memberId, MemberDto memberDto, IMemberUpdateService service) => {
            var result = await service.UpdateMemberAsync(memberId, memberDto);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}