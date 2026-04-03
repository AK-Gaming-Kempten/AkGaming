using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityClientEntry : ComponentBase
{
    [Parameter] public required OidcClientResponse Client { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public Func<OidcClientUpdateSubmission, Task<bool>>? OnSave { get; set; }
    [Parameter] public Func<string, Task<bool>>? OnDelete { get; set; }

    private bool EditMode { get; set; }
    private string _displayName = string.Empty;
    private string _clientType = "confidential";
    private string _consentType = "explicit";
    private string _clientSecret = string.Empty;
    private string _redirectUrisText = string.Empty;
    private string _postLogoutRedirectUrisText = string.Empty;
    private string _scopesText = string.Empty;
    private bool _requirePkce = true;
    private bool _allowRefreshTokenFlow = true;

    protected override void OnParametersSet()
    {
        if (!EditMode)
        {
            ResetEditor();
        }
    }

    private void ToggleEditMode()
    {
        EditMode = !EditMode;
        if (!EditMode)
        {
            ResetEditor();
        }
    }

    private void CancelEditing()
    {
        ResetEditor();
        EditMode = false;
    }

    private async Task SaveAsync()
    {
        var request = new AdminUpdateOidcClientRequest(
            DisplayName: _displayName.Trim(),
            ClientType: _clientType,
            ConsentType: _consentType,
            RequirePkce: _requirePkce,
            AllowAuthorizationCodeFlow: true,
            AllowRefreshTokenFlow: _allowRefreshTokenFlow,
            NewClientSecret: string.IsNullOrWhiteSpace(_clientSecret) ? null : _clientSecret.Trim(),
            RedirectUris: ParseMultiline(_redirectUrisText),
            PostLogoutRedirectUris: ParseMultiline(_postLogoutRedirectUrisText),
            Scopes: ParseMultiline(_scopesText));

        if (OnSave is null)
        {
            return;
        }

        var didSave = await OnSave(new OidcClientUpdateSubmission(Client.ClientId, request));
        if (didSave)
        {
            EditMode = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (OnDelete is null)
        {
            return;
        }

        var didDelete = await OnDelete(Client.ClientId);
        if (didDelete)
        {
            EditMode = false;
        }
    }

    private void ResetEditor()
    {
        _displayName = Client.DisplayName;
        _clientType = Client.ClientType;
        _consentType = Client.ConsentType;
        _clientSecret = string.Empty;
        _redirectUrisText = string.Join(Environment.NewLine, Client.RedirectUris ?? []);
        _postLogoutRedirectUrisText = string.Join(Environment.NewLine, Client.PostLogoutRedirectUris ?? []);
        _scopesText = string.Join(Environment.NewLine, Client.Scopes ?? []);
        _requirePkce = Client.RequirePkce;
        _allowRefreshTokenFlow = Client.AllowRefreshTokenFlow;
    }

    private string GetProtectionMessage() => Client.ProtectionReason ?? "This client is protected.";

    private static string[] ParseMultiline(string? value)
    {
        return (value ?? string.Empty)
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record OidcClientUpdateSubmission(string ClientId, AdminUpdateOidcClientRequest Request);
