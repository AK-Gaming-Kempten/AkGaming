using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Management.Frontend.Components.Administration.Disbursements;

public partial class ReimbursementsAdministrationPage : ComponentBase
{
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    private List<ReimbursementDto>? _items;
    private string _filter = string.Empty;
    private bool _busy;
    private string? _error;
    private IEnumerable<ReimbursementDto> FilteredItems => string.IsNullOrWhiteSpace(_filter) ? _items ?? [] : (_items ?? []).Where(item => item.Status.ToString() == _filter);

    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync()
    {
        _busy = true; _error = null;
        var result = await Api.GetAllReimbursementsAsync();
        _busy = false;
        if (result.IsSuccess) _items = result.Value?.ToList() ?? []; else { _items = []; _error = result.Error; }
    }
    private async Task UpdateStatusAsync(ReimbursementDto item, UpdateReimbursementStatusRequest request)
    {
        _busy = true; _error = null;
        var result = await Api.UpdateReimbursementStatusAsync(item.Id, request);
        _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        var index = _items!.FindIndex(existing => existing.Id == item.Id);
        _items[index] = result.Value!;
    }
    private async Task DownloadReceiptAsync(ReceiptDto receipt)
    {
        var result = await Api.DownloadReceiptAsync(receipt.Id, true);
        if (!result.IsSuccess) { _error = result.Error; return; }
        await Js.InvokeVoidAsync("akGaming.downloadFile", receipt.FileName, receipt.ContentType, Convert.ToBase64String(result.Value!));
    }
}
