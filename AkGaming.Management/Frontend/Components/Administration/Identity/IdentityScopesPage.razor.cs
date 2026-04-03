using AkGaming.Identity.Contracts.Auth;
using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityScopesPage : ComponentBase
{
    [Inject] private IdentityApiClient IdentityApi { get; set; } = default!;

    private List<OidcScopeResponse>? _scopes;

    private string _newScopeName = string.Empty;
    private string _newDisplayName = string.Empty;
    private string _newDescription = string.Empty;
    private string _newResourcesText = string.Empty;

    private string? _error;
    private string? _success;
    private bool _isBusy;
    private bool _showCreateForm;

    private IEnumerable<OidcScopeResponse> SortedScopes =>
        (_scopes ?? [])
            .OrderBy(scope => scope.Name, StringComparer.OrdinalIgnoreCase);

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

        _showCreateForm = false;
        ResetCreateForm();
        await LoadScopesAsync();
        _success = $"Created scope '{result.Value.Name}'.";
    }

    private async Task<bool> SaveScopeAsync(OidcScopeUpdateSubmission submission)
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.UpdateOidcScopeAsync(submission.ScopeName, submission.Request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return false;
        }

        await LoadScopesAsync();
        _success = $"Updated scope '{result.Value.Name}'.";
        return true;
    }

    private async Task<bool> DeleteScopeAsync(string scopeName)
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.DeleteOidcScopeAsync(scopeName);

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return false;
        }

        await LoadScopesAsync();
        _success = $"Deleted scope '{scopeName}'.";
        return true;
    }

    private void ResetCreateForm()
    {
        _newScopeName = string.Empty;
        _newDisplayName = string.Empty;
        _newDescription = string.Empty;
        _newResourcesText = string.Empty;
    }

    private void ToggleCreateForm()
    {
        _showCreateForm = !_showCreateForm;
        if (!_showCreateForm)
        {
            ResetCreateForm();
        }

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
