using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityRolesPage : ComponentBase {
    [Inject]
    private IdentityApiClient IdentityApi { get; set; } = default!;

    private List<RoleResponse>? _roles;
    private Guid? _selectedRoleId;
    private RoleResponse? _selectedRole;

    private string _newRoleName = string.Empty;
    private string _renameRoleName = string.Empty;

    private string? _error;
    private string? _success;
    private bool _isBusy;

    protected override async Task OnInitializedAsync() {
        await LoadRolesAsync();
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
            }
            else {
                _selectedRole = existing;
                _renameRoleName = existing.Name;
            }
        }
    }

    private void SelectRole(RoleResponse role) {
        _selectedRoleId = role.Id;
        _selectedRole = role;
        _renameRoleName = role.Name;
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
        _success = $"Deleted role '{roleName}'.";

        await LoadRolesAsync();
    }
}
