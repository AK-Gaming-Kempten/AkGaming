using AkGaming.Identity.Contracts.Auth;
using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityClientsPage : ComponentBase
{
    [Inject] private IdentityApiClient IdentityApi { get; set; } = default!;

    private List<OidcClientResponse>? _clients;

    private string _newClientId = string.Empty;
    private string _newDisplayName = string.Empty;
    private string _newClientType = "confidential";
    private string _newConsentType = "explicit";
    private string _newClientSecret = string.Empty;
    private string _newRedirectUrisText = string.Empty;
    private string _newPostLogoutRedirectUrisText = string.Empty;
    private string _newScopesText = "openid\nprofile\nemail\nroles\noffline_access";
    private bool _newRequirePkce = true;
    private bool _newAllowRefreshTokenFlow = true;

    private string? _error;
    private string? _success;
    private bool _isBusy;
    private bool _showCreateForm;

    private IEnumerable<OidcClientResponse> SortedClients =>
        (_clients ?? [])
            .OrderBy(client => client.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(client => client.ClientId, StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await LoadClientsAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.GetOidcClientsAsync();

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _clients = new List<OidcClientResponse>();
            _error = result.Error;
            return;
        }

        _clients = result.Value?.ToList() ?? new List<OidcClientResponse>();
    }

    private async Task CreateClientAsync()
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var request = new AdminCreateOidcClientRequest(
            ClientId: _newClientId.Trim(),
            ClientSecret: string.IsNullOrWhiteSpace(_newClientSecret) ? null : _newClientSecret.Trim(),
            DisplayName: _newDisplayName.Trim(),
            ClientType: _newClientType,
            ConsentType: _newConsentType,
            RequirePkce: _newRequirePkce,
            AllowAuthorizationCodeFlow: true,
            AllowRefreshTokenFlow: _newAllowRefreshTokenFlow,
            RedirectUris: ParseMultiline(_newRedirectUrisText),
            PostLogoutRedirectUris: ParseMultiline(_newPostLogoutRedirectUrisText),
            Scopes: ParseMultiline(_newScopesText));

        var result = await IdentityApi.CreateOidcClientAsync(request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return;
        }

        _showCreateForm = false;
        ResetCreateForm();
        await LoadClientsAsync();
        _success = $"Created client '{result.Value.ClientId}'.";
    }

    private async Task<bool> SaveClientAsync(OidcClientUpdateSubmission submission)
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.UpdateOidcClientAsync(submission.ClientId, submission.Request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return false;
        }

        await LoadClientsAsync();
        _success = $"Updated client '{result.Value.ClientId}'.";
        return true;
    }

    private async Task<bool> DeleteClientAsync(string clientId)
    {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.DeleteOidcClientAsync(clientId);

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return false;
        }

        await LoadClientsAsync();
        _success = $"Deleted client '{clientId}'.";
        return true;
    }

    private void ResetCreateForm()
    {
        _newClientId = string.Empty;
        _newDisplayName = string.Empty;
        _newClientType = "confidential";
        _newConsentType = "explicit";
        _newClientSecret = string.Empty;
        _newRedirectUrisText = string.Empty;
        _newPostLogoutRedirectUrisText = string.Empty;
        _newScopesText = "openid\nprofile\nemail\nroles\noffline_access";
        _newRequirePkce = true;
        _newAllowRefreshTokenFlow = true;
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
