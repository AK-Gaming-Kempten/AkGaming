using AkGaming.Identity.Contracts.Auth;
using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityScopesPage : ComponentBase
{
    [Inject] private IdentityApiClient IdentityApi { get; set; } = default!;

    private List<OidcScopeResponse>? _scopes;
    private string? _selectedScopeName;
    private OidcScopeResponse? _selectedScope;

    private string _newScopeName = string.Empty;
    private string _newDisplayName = string.Empty;
    private string _newDescription = string.Empty;
    private string _newResourcesText = string.Empty;

    private string _editDisplayName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editResourcesText = string.Empty;

    private string? _error;
    private string? _success;
    private bool _isBusy;
    private bool _isMobileDetailOpen;

    protected override async Task OnInitializedAsync()
    {
        await LoadScopesAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadScopesAsync();
    }

    private async Task LoadScopesAsync()
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.GetOidcScopesAsync();

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _scopes = new List<OidcScopeResponse>();
            _error = result.Error;
            return;
        }

        _scopes = result.Value?.ToList() ?? new List<OidcScopeResponse>();

        if (!string.IsNullOrWhiteSpace(_selectedScopeName))
        {
            var selected = _scopes.FirstOrDefault(scope => string.Equals(scope.Name, _selectedScopeName, StringComparison.Ordinal));
            if (selected is null)
            {
                ShowListMobile();
            }
            else
            {
                ApplyScopeSelection(selected);
            }
        }
    }

    private void SelectScope(OidcScopeResponse scope)
    {
        ApplyScopeSelection(scope);
        _isMobileDetailOpen = true;
        _error = null;
        _success = null;
    }

    private void ApplyScopeSelection(OidcScopeResponse scope)
    {
        _selectedScope = scope;
        _selectedScopeName = scope.Name;
        _editDisplayName = scope.DisplayName;
        _editDescription = scope.Description ?? string.Empty;
        _editResourcesText = string.Join(Environment.NewLine, scope.Resources ?? Array.Empty<string>());
    }

    private async Task CreateScopeAsync()
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var request = new AdminCreateOidcScopeRequest(
            Name: _newScopeName.Trim(),
            DisplayName: _newDisplayName.Trim(),
            Description: string.IsNullOrWhiteSpace(_newDescription) ? null : _newDescription.Trim(),
            Resources: ParseMultiline(_newResourcesText));

        var result = await IdentityApi.CreateOidcScopeAsync(request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return;
        }

        _success = $"Created scope '{result.Value.Name}'.";
        ResetCreateForm();
        await LoadScopesAsync();
        SelectScope(result.Value);
    }

    private async Task SaveScopeAsync()
    {
        if (_selectedScope is null)
        {
            _error = "Select a scope first.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var request = new AdminUpdateOidcScopeRequest(
            DisplayName: _editDisplayName.Trim(),
            Description: string.IsNullOrWhiteSpace(_editDescription) ? null : _editDescription.Trim(),
            Resources: ParseMultiline(_editResourcesText));

        var result = await IdentityApi.UpdateOidcScopeAsync(_selectedScope.Name, request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return;
        }

        _success = $"Updated scope '{result.Value.Name}'.";
        await LoadScopesAsync();
        SelectScope(result.Value);
    }

    private async Task DeleteScopeAsync()
    {
        if (_selectedScope is null)
        {
            _error = "Select a scope first.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var scopeName = _selectedScope.Name;
        var result = await IdentityApi.DeleteOidcScopeAsync(scopeName);

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _success = $"Deleted scope '{scopeName}'.";
        ShowListMobile();
        await LoadScopesAsync();
    }

    private void ResetCreateForm()
    {
        _newScopeName = string.Empty;
        _newDisplayName = string.Empty;
        _newDescription = string.Empty;
        _newResourcesText = string.Empty;
    }

    private void ShowListMobile()
    {
        _isMobileDetailOpen = false;
        _selectedScopeName = null;
        _selectedScope = null;
        _editDisplayName = string.Empty;
        _editDescription = string.Empty;
        _editResourcesText = string.Empty;
        _error = null;
        _success = null;
    }

    private static string[] ParseMultiline(string? value)
    {
        return (value ?? string.Empty)
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
