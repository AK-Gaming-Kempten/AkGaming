using System.Security.Claims;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MemberUpdateEndpoints {
    public static IEndpointRouteBuilder MapMemberUpdateEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands");

        group.MapPut("/{memberId}", async (Guid memberId, MemberDto memberDto, ClaimsPrincipal user, IMemberUpdateService service) => {
            var result = await service.UpdateMemberAsync(memberId, memberDto, GetCurrentUserIdOrNull(user));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOrSelfRouteUserId");

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }
}
