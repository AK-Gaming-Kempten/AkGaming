using AkGaming.Identity.Contracts.Auth;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Identity;

public partial class IdentityScopeEntry : ComponentBase
{
    [Parameter] public required OidcScopeResponse Scope { get; set; }
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public Func<OidcScopeUpdateSubmission, Task<bool>>? OnSave { get; set; }
    [Parameter] public Func<string, Task<bool>>? OnDelete { get; set; }

    private bool EditMode { get; set; }
    private string _displayName = string.Empty;
    private string _description = string.Empty;
    private string _resourcesText = string.Empty;

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
        var request = new AdminUpdateOidcScopeRequest(
            DisplayName: _displayName.Trim(),
            Description: string.IsNullOrWhiteSpace(_description) ? null : _description.Trim(),
            Resources: ParseMultiline(_resourcesText));

        if (OnSave is null)
        {
            return;
        }

        var didSave = await OnSave(new OidcScopeUpdateSubmission(Scope.Name, request));
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

        var didDelete = await OnDelete(Scope.Name);
        if (didDelete)
        {
            EditMode = false;
        }
    }

    private void ResetEditor()
    {
        _displayName = Scope.DisplayName;
        _description = Scope.Description ?? string.Empty;
        _resourcesText = string.Join(Environment.NewLine, Scope.Resources ?? []);
    }

    private string GetProtectionMessage() => Scope.ProtectionReason ?? "This scope is protected.";

    private static string[] ParseMultiline(string? value)
    {
        return (value ?? string.Empty)
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed record OidcScopeUpdateSubmission(string ScopeName, AdminUpdateOidcScopeRequest Request);
