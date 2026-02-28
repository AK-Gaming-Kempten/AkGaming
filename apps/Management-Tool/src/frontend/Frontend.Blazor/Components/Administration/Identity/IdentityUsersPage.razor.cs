using Frontend.Blazor.ApiClients;
using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.Identity;

public partial class IdentityUsersPage : ComponentBase {
    [Inject]
    private IdentityApiClient IdentityApi { get; set; } = default!;

    private readonly List<RoleResponse> _availableRoles = new();
    private readonly HashSet<string> _selectedRoles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _currentRoles = new(StringComparer.OrdinalIgnoreCase);

    private AdminUsersResponse? _usersResponse;
    private AdminUserDetailsResponse? _selectedUserDetails;
    private Guid? _selectedUserId;

    private int _page = 1;
    private int _pageSize = 25;
    private int _totalPages = 1;
    private string _search = string.Empty;

    private string? _error;
    private string? _success;
    private bool _isBusy;

    protected override async Task OnInitializedAsync() {
        await LoadRolesCatalogAsync();
        await LoadUsersAsync();
    }

    private async Task LoadRolesCatalogAsync() {
        var rolesResult = await IdentityApi.GetRolesAsync();
        if (!rolesResult.IsSuccess || rolesResult.Value is null) {
            _error = rolesResult.Error;
            return;
        }

        _availableRoles.Clear();
        _availableRoles.AddRange(rolesResult.Value.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase));
    }

    private async Task ReloadUsersAsync() {
        await LoadUsersAsync();
    }

    private async Task SearchAsync() {
        _page = 1;
        await LoadUsersAsync();
    }

    private async Task PrevPageAsync() {
        if (_page <= 1) {
            return;
        }

        _page -= 1;
        await LoadUsersAsync();
    }

    private async Task NextPageAsync() {
        if (_page >= _totalPages) {
            return;
        }

        _page += 1;
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync() {
        _isBusy = true;
        _error = null;
        _success = null;

        if (_pageSize <= 0) {
            _pageSize = 25;
        }
        if (_pageSize > 200) {
            _pageSize = 200;
        }

        var usersResult = await IdentityApi.GetAdminUsersAsync(_page, _pageSize, _search);

        _isBusy = false;

        if (!usersResult.IsSuccess || usersResult.Value is null) {
            _usersResponse = new AdminUsersResponse(1, _pageSize, 0, Array.Empty<AdminUserListItemResponse>());
            _totalPages = 1;
            _error = usersResult.Error;
            return;
        }

        _usersResponse = usersResult.Value;
        _page = _usersResponse.Page <= 0 ? 1 : _usersResponse.Page;
        _pageSize = _usersResponse.PageSize <= 0 ? _pageSize : _usersResponse.PageSize;
        _totalPages = Math.Max(1, (int)Math.Ceiling(_usersResponse.TotalCount / (double)Math.Max(1, _pageSize)));

        if (_selectedUserId.HasValue && _usersResponse.Items.All(u => u.UserId != _selectedUserId.Value)) {
            _selectedUserDetails = null;
            _selectedUserId = null;
            _selectedRoles.Clear();
            _currentRoles.Clear();
        }
    }

    private async Task SelectUserAsync(Guid userId) {
        _isBusy = true;
        _error = null;
        _success = null;

        var detailsResult = await IdentityApi.GetAdminUserDetailsAsync(userId);
        var rolesResult = await IdentityApi.GetUserRolesAsync(userId);

        _isBusy = false;

        if (!detailsResult.IsSuccess || detailsResult.Value is null) {
            _error = detailsResult.Error;
            return;
        }

        _selectedUserId = userId;
        _selectedUserDetails = detailsResult.Value;

        _currentRoles.Clear();
        _selectedRoles.Clear();

        var effectiveRoles = rolesResult.IsSuccess && rolesResult.Value is not null
            ? rolesResult.Value.Roles
            : _selectedUserDetails.Roles;

        foreach (var role in effectiveRoles ?? Array.Empty<string>()) {
            if (string.IsNullOrWhiteSpace(role)) {
                continue;
            }

            _currentRoles.Add(role);
            _selectedRoles.Add(role);

            if (_availableRoles.All(r => !string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase))) {
                _availableRoles.Add(new RoleResponse(Guid.Empty, role));
            }
        }

        _availableRoles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        if (!rolesResult.IsSuccess) {
            _error = rolesResult.Error;
        }
    }

    private void ToggleRole(string roleName, bool isSelected) {
        if (isSelected) {
            _selectedRoles.Add(roleName);
        }
        else {
            _selectedRoles.Remove(roleName);
        }
    }

    private async Task SaveRolesAsync() {
        _error = null;
        _success = null;

        if (!_selectedUserId.HasValue) {
            _error = "Select a user first.";
            return;
        }

        _isBusy = true;

        var response = await IdentityApi.SetUserRolesAsync(
            _selectedUserId.Value,
            new AdminSetUserRolesRequest(_selectedRoles.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToArray()));

        _isBusy = false;

        if (!response.IsSuccess || response.Value is null) {
            _error = response.Error;
            return;
        }

        _currentRoles.Clear();
        foreach (var role in response.Value.Roles ?? Array.Empty<string>()) {
            _currentRoles.Add(role);
        }

        _success = "User roles updated.";
    }
}
