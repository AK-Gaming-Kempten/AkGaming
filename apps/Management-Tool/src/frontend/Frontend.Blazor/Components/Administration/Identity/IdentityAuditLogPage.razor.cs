using AkGaming.Identity.Contracts.Auth;
using Frontend.Blazor.ApiClients;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.Identity;

public partial class IdentityAuditLogPage : ComponentBase {
    [Inject]
    private IdentityApiClient IdentityApi { get; set; } = default!;

    private AdminAuditLogsResponse? _auditLogs;

    private int _page = 1;
    private int _pageSize = 14;
    private int _totalPages = 1;
    private string _search = string.Empty;

    private string? _error;
    private string? _success;
    private bool _isBusy;

    protected override async Task OnInitializedAsync() {
        await LoadAuditLogsAsync();
    }

    private async Task ReloadAsync() {
        await LoadAuditLogsAsync();
    }

    private async Task SearchAsync() {
        _page = 1;
        await LoadAuditLogsAsync();
    }
    
    private Task OnSearchChanged(string value) {
        _search = value;
        return Task.CompletedTask;
    }

    private Task OnPageSizeChanged(int value) {
        _pageSize = value;
        return Task.CompletedTask;
    }

    private async Task PrevPageAsync() {
        if (_page <= 1) {
            return;
        }

        _page -= 1;
        await LoadAuditLogsAsync();
    }

    private async Task NextPageAsync() {
        if (_page >= _totalPages) {
            return;
        }

        _page += 1;
        await LoadAuditLogsAsync();
    }

    private async Task LoadAuditLogsAsync() {
        _isBusy = true;
        _error = null;
        _success = null;

        if (_pageSize <= 0) {
            _pageSize = 25;
        }

        if (_pageSize > 200) {
            _pageSize = 200;
        }

        var result = await IdentityApi.GetAdminAuditLogsAsync(_page, _pageSize, _search);

        _isBusy = false;

        if (!result.IsSuccess || result.Value is null) {
            _auditLogs = new AdminAuditLogsResponse(1, _pageSize, 0, Array.Empty<AdminAuditLogItemResponse>());
            _totalPages = 1;
            _error = result.Error;
            return;
        }

        _auditLogs = result.Value;
        _page = _auditLogs.Page <= 0 ? 1 : _auditLogs.Page;
        _pageSize = _auditLogs.PageSize <= 0 ? _pageSize : _auditLogs.PageSize;
        _totalPages = Math.Max(1, (int)Math.Ceiling(_auditLogs.TotalCount / (double)Math.Max(1, _pageSize)));
        _success = "Audit logs loaded.";
    }
}
