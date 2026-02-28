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

        group.MapPut("/{memberId:guid}", async (
            Guid memberId,
            MemberDto memberDto,
            ClaimsPrincipal user,
            IMemberQueryService queryService,
            IMemberUpdateService service
        ) => {
            if (memberDto.Id != Guid.Empty && memberDto.Id != memberId) {
                return Results.BadRequest("Route memberId does not match payload member id.");
            }

            var currentUserId = GetCurrentUserIdOrNull(user);
            if (!user.IsInRole("Admin")) {
                if (!currentUserId.HasValue) {
                    return Results.Forbid();
                }

                var memberResult = await queryService.GetMemberByGuidAsync(memberId);
                if (!memberResult.IsSuccess || memberResult.Value is null) {
                    return Results.NotFound(memberResult.Error);
                }

                if (memberResult.Value.UserId != currentUserId.Value) {
                    return Results.Forbid();
                }
            }

            memberDto.Id = memberId;
            var result = await service.UpdateMemberAsync(memberId, memberDto, currentUserId);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        return endpoints;
    }

    private static Guid? GetCurrentUserIdOrNull(ClaimsPrincipal user) {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var currentUserId) ? currentUserId : null;
    }
}
