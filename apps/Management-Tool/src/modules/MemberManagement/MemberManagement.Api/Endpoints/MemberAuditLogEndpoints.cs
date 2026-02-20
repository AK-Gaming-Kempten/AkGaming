using MemberManagement.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api.Endpoints;

public static class MemberAuditLogEndpoints {
    public static IEndpointRouteBuilder MapMemberAuditLogEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/members")
            .WithTags("Members - Audit Logs");

        group.MapGet("/audit-logs", async (
            [FromServices] IMemberAuditLogService service,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null
        ) => {
            var result = await service.GetAuditLogsAsync(page, pageSize, search);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("AdminOnly");

        return endpoints;
    }
}
