using AkGaming.Identity.Contracts.Auth;
using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityClientsPage : ComponentBase
{
    [Inject] private IdentityApiClient IdentityApi { get; set; } = default!;

    private List<OidcClientResponse>? _clients;
    private string? _selectedClientId;
    private OidcClientResponse? _selectedClient;

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

    private string _editDisplayName = string.Empty;
    private string _editClientType = "confidential";
    private string _editConsentType = "explicit";
    private string _editClientSecret = string.Empty;
    private string _editRedirectUrisText = string.Empty;
    private string _editPostLogoutRedirectUrisText = string.Empty;
    private string _editScopesText = string.Empty;
    private bool _editRequirePkce = true;
    private bool _editAllowRefreshTokenFlow = true;

    private string? _error;
    private string? _success;
    private bool _isBusy;
    private bool _isMobileDetailOpen;

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

        if (!string.IsNullOrWhiteSpace(_selectedClientId))
        {
            var selected = _clients.FirstOrDefault(client => string.Equals(client.ClientId, _selectedClientId, StringComparison.Ordinal));
            if (selected is null)
            {
                ShowListMobile();
            }
            else
            {
                ApplyClientSelection(selected);
            }
        }
    }

    private void SelectClient(OidcClientResponse client)
    {
        ApplyClientSelection(client);
        _isMobileDetailOpen = true;
        _error = null;
        _success = null;
    }

    private void ApplyClientSelection(OidcClientResponse client)
    {
        _selectedClient = client;
        _selectedClientId = client.ClientId;
        _editDisplayName = client.DisplayName;
        _editClientType = client.ClientType;
        _editConsentType = client.ConsentType;
        _editClientSecret = string.Empty;
        _editRedirectUrisText = string.Join(Environment.NewLine, client.RedirectUris ?? Array.Empty<string>());
        _editPostLogoutRedirectUrisText = string.Join(Environment.NewLine, client.PostLogoutRedirectUris ?? Array.Empty<string>());
        _editScopesText = string.Join(Environment.NewLine, client.Scopes ?? Array.Empty<string>());
        _editRequirePkce = client.RequirePkce;
        _editAllowRefreshTokenFlow = client.AllowRefreshTokenFlow;
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

        _success = $"Created client '{result.Value.ClientId}'.";
        ResetCreateForm();
        await LoadClientsAsync();
        SelectClient(result.Value);
    }

    private async Task SaveClientAsync()
    {
        if (_selectedClient is null)
        {
            _error = "Select a client first.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var request = new AdminUpdateOidcClientRequest(
            DisplayName: _editDisplayName.Trim(),
            ClientType: _editClientType,
            ConsentType: _editConsentType,
            RequirePkce: _editRequirePkce,
            AllowAuthorizationCodeFlow: true,
            AllowRefreshTokenFlow: _editAllowRefreshTokenFlow,
            NewClientSecret: string.IsNullOrWhiteSpace(_editClientSecret) ? null : _editClientSecret.Trim(),
            RedirectUris: ParseMultiline(_editRedirectUrisText),
            PostLogoutRedirectUris: ParseMultiline(_editPostLogoutRedirectUrisText),
            Scopes: ParseMultiline(_editScopesText));

        var result = await IdentityApi.UpdateOidcClientAsync(_selectedClient.ClientId, request);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            _error = result.Error;
            return;
        }

        _success = $"Updated client '{result.Value.ClientId}'.";
        await LoadClientsAsync();
        SelectClient(result.Value);
    }

    private async Task DeleteClientAsync()
    {
        if (_selectedClient is null)
        {
            _error = "Select a client first.";
            _success = null;
            return;
        }

        _isBusy = true;
        _error = null;
        _success = null;

        var clientId = _selectedClient.ClientId;
        var result = await IdentityApi.DeleteOidcClientAsync(clientId);

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            return;
        }

        _success = $"Deleted client '{clientId}'.";
        ShowListMobile();
        await LoadClientsAsync();
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

    private void ShowListMobile()
    {
        _isMobileDetailOpen = false;
        _selectedClientId = null;
        _selectedClient = null;
        _editDisplayName = string.Empty;
        _editClientType = "confidential";
        _editConsentType = "explicit";
        _editClientSecret = string.Empty;
        _editRedirectUrisText = string.Empty;
        _editPostLogoutRedirectUrisText = string.Empty;
        _editScopesText = string.Empty;
        _editRequirePkce = true;
        _editAllowRefreshTokenFlow = true;
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
