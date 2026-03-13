using AkGaming.Management.Modules.MemberManagement.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace AkGaming.Management.Modules.MemberManagement.Api;

public static class MemberManagementApiModule {
    public static IEndpointRouteBuilder MapMemberManagementEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapMemberQueryEndpoints();
        endpoints.MapMemberAuditLogEndpoints();
        endpoints.MapMemberCreationEndpoints();
        endpoints.MapMemberUpdateEndpoints();
        endpoints.MapMemberDeletionEndpoints();
        endpoints.MapMemberLinkingEndpoints();
        endpoints.MapMembershipUpdateEndpoints();
        endpoints.MapMembershipApplicationEndpoints();
        endpoints.MapMembershipDueEndpoints();
        return endpoints;
    }
}
