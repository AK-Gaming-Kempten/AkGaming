using System.Text.Json;
using AKG.Common.Generics;
using AkGaming.Identity.Contracts.Auth;

namespace Frontend.Blazor.ApiClients;

public sealed class IdentityApiClient : ApiClientBase {
    private readonly string _defaultAuditLogPath;

    public IdentityApiClient(HttpClient http, IConfiguration config) : base(http) {
        _defaultAuditLogPath = config["IdentityApi:AuditLogPath"] ?? "/admin/audit-log";
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

    public Task<Result<JsonElement>> GetAuditLogAsync(string? path = null, CancellationToken ct = default) {
        var effectivePath = string.IsNullOrWhiteSpace(path) ? _defaultAuditLogPath : path.Trim();
        if (!effectivePath.StartsWith('/')) {
            effectivePath = "/" + effectivePath;
        }

        return GetAsync<JsonElement>(effectivePath, ct);
    }
}
