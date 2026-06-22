using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityRolesPage : ComponentBase {
    [Inject]
    private IdentityApiClient IdentityApi { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private List<RoleResponse>? _roles;
    private List<PermissionResponse>? _permissions;
    private Guid? _selectedRoleId;
    private RoleResponse? _selectedRole;

    private string _newRoleName = string.Empty;
    private string _renameRoleName = string.Empty;
    private bool _isMobileDetailOpen;

    private string? _error;
    private string? _success;
    private bool _isBusy;
    private bool _canManageRoles;
    private readonly HashSet<string> _selectedPermissionKeys = new(StringComparer.Ordinal);
    private bool IsSelectedRoleSystemRole => _selectedRole?.Name is "Admin" or "User";

    protected override async Task OnInitializedAsync() {
        var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _canManageRoles = authenticationState.User.HasClaim("permission", "identity.roles.manage");
        await Task.WhenAll(LoadRolesAsync(), LoadPermissionsAsync());
    }

    private async Task ReloadAsync() {
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync() {
        _error = null;
        _success = null;

        var result = await IdentityApi.GetRolesAsync();
        if (!result.IsSuccess) {
            _roles = new List<RoleResponse>();
            _error = result.Error;
            return;
        }

        _roles = result.Value?.ToList() ?? new List<RoleResponse>();

        if (_selectedRoleId.HasValue) {
            var existing = _roles.FirstOrDefault(r => r.Id == _selectedRoleId.Value);
            if (existing is null) {
                _selectedRoleId = null;
                _selectedRole = null;
                _renameRoleName = string.Empty;
                _selectedPermissionKeys.Clear();
            }
            else {
                _selectedRole = existing;
                _renameRoleName = existing.Name;
                SetSelectedPermissions(existing);
            }
        }
    }

    private async Task LoadPermissionsAsync() {
        var result = await IdentityApi.GetPermissionsAsync();
        if (!result.IsSuccess) {
            _permissions = new List<PermissionResponse>();
            _error = result.Error;
            return;
        }

        _permissions = result.Value?.OrderBy(permission => permission.Key, StringComparer.Ordinal).ToList() ?? new List<PermissionResponse>();
    }

    private void SelectRole(RoleResponse role) {
        _selectedRoleId = role.Id;
        _selectedRole = role;
        _renameRoleName = role.Name;
        SetSelectedPermissions(role);
        _isMobileDetailOpen = true;
        _error = null;
        _success = null;
    }

    private async Task CreateRoleAsync() {
        var roleName = (_newRoleName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(roleName)) {
            _error = "Role name is required.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.CreateRoleAsync(new AdminCreateRoleRequest(roleName));

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null) {
            _error = result.Error;
            return;
        }

        _newRoleName = string.Empty;
        _success = $"Created role '{result.Value.Name}'.";

        await LoadRolesAsync();
        SelectRole(result.Value);
    }

    private async Task RenameRoleAsync() {
        if (_selectedRole is null) {
            _error = "Select a role first.";
            _success = null;
            return;
        }

        var roleName = (_renameRoleName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(roleName)) {
            _error = "Role name is required.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.RenameRoleAsync(_selectedRole.Id, new AdminRenameRoleRequest(roleName));

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null) {
            _error = result.Error;
            return;
        }

        _success = $"Renamed role to '{result.Value.Name}'.";
        await LoadRolesAsync();
        SelectRole(result.Value);
    }

    private void SetPermissionSelection(string permissionKey, object? value) {
        if (value is not bool isSelected || IsSelectedRoleSystemRole || !_canManageRoles) {
            return;
        }

        if (isSelected) {
            _selectedPermissionKeys.Add(permissionKey);
        }
        else {
            _selectedPermissionKeys.Remove(permissionKey);
        }
    }

    private async Task SavePermissionsAsync() {
        if (_selectedRole is null || IsSelectedRoleSystemRole || !_canManageRoles) {
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;
        var result = await IdentityApi.SetRolePermissionsAsync(
            _selectedRole.Id,
            new AdminSetRolePermissionsRequest(_selectedPermissionKeys.OrderBy(key => key, StringComparer.Ordinal).ToArray()));
        _isBusy = false;

        if (!result.IsSuccess || result.Value is null) {
            _error = result.Error;
            return;
        }

        _success = "Updated role permissions.";
        await LoadRolesAsync();
        SelectRole(result.Value);
    }

    private async Task DeleteRoleAsync() {
        if (_selectedRole is null) {
            _error = "Select a role first.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var roleName = _selectedRole.Name;
        var roleId = _selectedRole.Id;
        var result = await IdentityApi.DeleteRoleAsync(roleId);

        _isBusy = false;

        if (!result.IsSuccess) {
            _error = result.Error;
            return;
        }

        _selectedRole = null;
        _selectedRoleId = null;
        _renameRoleName = string.Empty;
        _selectedPermissionKeys.Clear();
        _success = $"Deleted role '{roleName}'.";

        await LoadRolesAsync();
        _isMobileDetailOpen = false;
    }

    private void ShowListMobile() {
        _isMobileDetailOpen = false;
        _selectedRoleId = null;
        _selectedRole = null;
        _renameRoleName = string.Empty;
        _error = null;
        _success = null;
    }

    private void SetSelectedPermissions(RoleResponse role) {
        _selectedPermissionKeys.Clear();
        foreach (var permission in role.Permissions) {
            _selectedPermissionKeys.Add(permission);
        }
    }

    private IEnumerable<IGrouping<string, PermissionResponse>> GetPermissionApplications() {
        return (_permissions ?? [])
            .GroupBy(permission => permission.Application)
            .OrderBy(group => group.Key, StringComparer.Ordinal);
    }

    private static string ToDisplayText(string value) {
        return string.Join(
            ' ',
            value
                .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
