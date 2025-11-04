using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;
using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MembershipUpdateEndpoints {
    public static IEndpointRouteBuilder MapMembershipUpdateEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Commands")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{memberId}/updateStatus", async ([FromRoute] Guid memberId, MembershipStatus status, IMembershipUpdateService service) => {
            var result = await service.UpdateMembershipStatusAsync(memberId, status);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });
        
        group.MapPut("/insertStatusChangeEvent", async (
            MembershipStatusChangeEventDto changeEvent,
            IMembershipUpdateService service) => 
        {
            var result = await service.InsertMembershipStatusChangeEventAsync(changeEvent.MemberId, changeEvent.OldStatus, changeEvent.NewStatus, changeEvent.Timestamp);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });
        
        group.MapGet("/{memberId}/endOfTrial", async ([FromRoute] Guid memberId, IMembershipUpdateService service) => {
            var result = await service.GetDefaultEndOfTrialPeriodAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        group.MapGet("/{memberId}/statusChanges", async ([FromRoute] Guid memberId, IMembershipUpdateService service) => {
            var result = await service.GetMembershipStatusChangesAsync(memberId);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return endpoints;
    }
}