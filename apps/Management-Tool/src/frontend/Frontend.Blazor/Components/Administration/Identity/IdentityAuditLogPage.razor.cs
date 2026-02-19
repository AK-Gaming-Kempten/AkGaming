using System.Text.Json;
using Frontend.Blazor.ApiClients;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.Identity;

public partial class IdentityAuditLogPage : ComponentBase {
    [Inject]
    private IdentityApiClient IdentityApi { get; set; } = default!;

    private string _auditPath = string.Empty;

    private string? _prettyJson;
    private string? _metaText;
    private string? _error;
    private string? _success;
    private bool _isBusy;

    protected override async Task OnInitializedAsync() {
        await LoadAuditLogAsync();
    }

    private async Task LoadAuditLogAsync() {
        _isBusy = true;
        _error = null;
        _success = null;

        var result = await IdentityApi.GetAuditLogAsync(_auditPath);

        _isBusy = false;

        if (!result.IsSuccess) {
            _prettyJson = null;
            _metaText = null;
            _error = result.Error;
            return;
        }

        var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { WriteIndented = true });
        _prettyJson = json;

        _metaText = result.Value.ValueKind switch {
            JsonValueKind.Array => $"Received array with {result.Value.GetArrayLength()} entries.",
            JsonValueKind.Object => "Received object payload.",
            _ => $"Received {result.Value.ValueKind} payload."
        };

        _success = "Audit log loaded.";
    }
}
