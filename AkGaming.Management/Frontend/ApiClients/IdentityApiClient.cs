using AkGaming.Core.Common.Generics;
using AkGaming.Identity.Contracts.Auth;

namespace AkGaming.Management.Frontend.ApiClients;

public sealed class IdentityApiClient : ApiClientBase {
    public IdentityApiClient(HttpClient http, IConfiguration config) : base(http) { }

    public Task<Result<ICollection<OidcClientResponse>>> GetOidcClientsAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<OidcClientResponse>>("admin/oidc/clients", ct);

    public Task<Result<OidcClientResponse>> GetOidcClientAsync(string clientId, CancellationToken ct = default) =>
        GetAsync<OidcClientResponse>($"admin/oidc/clients/{Uri.EscapeDataString(clientId)}", ct);

    public Task<Result<OidcClientResponse>> CreateOidcClientAsync(AdminCreateOidcClientRequest request, CancellationToken ct = default) =>
        PostJsonAsync<AdminCreateOidcClientRequest, OidcClientResponse>("admin/oidc/clients", request, ct);

    public Task<Result<OidcClientResponse>> UpdateOidcClientAsync(string clientId, AdminUpdateOidcClientRequest request, CancellationToken ct = default) =>
        PutJsonAsync<AdminUpdateOidcClientRequest, OidcClientResponse>($"admin/oidc/clients/{Uri.EscapeDataString(clientId)}", request, ct);

    public async Task<Result> DeleteOidcClientAsync(string clientId, CancellationToken ct = default) {
        using var response = await Http.DeleteAsync($"admin/oidc/clients/{Uri.EscapeDataString(clientId)}", ct);
        return await ToResult(response, ct);
    }

    public Task<Result<ICollection<OidcScopeResponse>>> GetOidcScopesAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<OidcScopeResponse>>("admin/oidc/scopes", ct);

    public Task<Result<OidcScopeResponse>> GetOidcScopeAsync(string scopeName, CancellationToken ct = default) =>
        GetAsync<OidcScopeResponse>($"admin/oidc/scopes/{Uri.EscapeDataString(scopeName)}", ct);

    public Task<Result<OidcScopeResponse>> CreateOidcScopeAsync(AdminCreateOidcScopeRequest request, CancellationToken ct = default) =>
        PostJsonAsync<AdminCreateOidcScopeRequest, OidcScopeResponse>("admin/oidc/scopes", request, ct);

    public Task<Result<OidcScopeResponse>> UpdateOidcScopeAsync(string scopeName, AdminUpdateOidcScopeRequest request, CancellationToken ct = default) =>
        PutJsonAsync<AdminUpdateOidcScopeRequest, OidcScopeResponse>($"admin/oidc/scopes/{Uri.EscapeDataString(scopeName)}", request, ct);

    public async Task<Result> DeleteOidcScopeAsync(string scopeName, CancellationToken ct = default) {
        using var response = await Http.DeleteAsync($"admin/oidc/scopes/{Uri.EscapeDataString(scopeName)}", ct);
        return await ToResult(response, ct);
    }

    public Task<Result<ICollection<RoleResponse>>> GetRolesAsync(CancellationToken ct = default) =>
        GetAsync<ICollection<RoleResponse>>("admin/roles", ct);

    public Task<Result<RoleResponse>> CreateRoleAsync(AdminCreateRoleRequest request, CancellationToken ct = default) =>
        PostJsonAsync<AdminCreateRoleRequest, RoleResponse>("admin/roles", request, ct);

    public Task<Result<RoleResponse>> RenameRoleAsync(Guid roleId, AdminRenameRoleRequest request, CancellationToken ct = default) =>
        PutJsonAsync<AdminRenameRoleRequest, RoleResponse>($"admin/roles/{roleId}", request, ct);

    public async Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken ct = default) {
        using var response = await Http.DeleteAsync($"admin/roles/{roleId}", ct);
        return await ToResult(response, ct);
    }

    public Task<Result<UserRolesResponse>> GetUserRolesAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<UserRolesResponse>($"admin/users/{userId}/roles", ct);

    public Task<Result<UserRolesResponse>> SetUserRolesAsync(Guid userId, AdminSetUserRolesRequest request, CancellationToken ct = default) =>
        PutJsonAsync<AdminSetUserRolesRequest, UserRolesResponse>($"admin/users/{userId}/roles", request, ct);

    public Task<Result<AdminUsersResponse>> GetAdminUsersAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        CancellationToken ct = default) {
        var queryParts = new List<string> {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search)) {
            queryParts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        return GetAsync<AdminUsersResponse>($"admin/users?{string.Join("&", queryParts)}", ct);
    }

    public Task<Result<AdminUserDetailsResponse>> GetAdminUserDetailsAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<AdminUserDetailsResponse>($"admin/users/{userId}", ct);

    public Task<Result<AdminAuditLogsResponse>> GetAdminAuditLogsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        CancellationToken ct = default) {
        var queryParts = new List<string> {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search)) {
            queryParts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        return GetAsync<AdminAuditLogsResponse>($"admin/audit-logs?{string.Join("&", queryParts)}", ct);
    }
}
