using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Api.Endpoints;

public static class MemberCreationEndpoints {
    public static IEndpointRouteBuilder MapMemberCreationEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Queries")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", async (MemberCreationDto memberCreationDto, IMemberCreationService service) => {
            var result = await service.CreateMemberAsync(memberCreationDto);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}