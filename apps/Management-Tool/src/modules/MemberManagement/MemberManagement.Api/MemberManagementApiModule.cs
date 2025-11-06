using MemberManagement.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace MemberManagement.Api;

public static class MemberManagementApiModule {
    public static IEndpointRouteBuilder MapMemberManagementEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapMemberQueryEndpoints();
        endpoints.MapMemberCreationEndpoints();
        endpoints.MapMemberUpdateEndpoints();
        endpoints.MapMemberDeletionEndpoints();
        endpoints.MapMemberLinkingEndpoints();
        endpoints.MapMembershipUpdateEndpoints();
        endpoints.MapMembershipApplicationEndpoints();
        return endpoints;
    }
}