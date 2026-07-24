using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class MyReimbursementsPage : ComponentBase
{
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;
    private List<ReimbursementDto>? _items;
    private List<PaymentInformationDto> _paymentMethods = [];
    private CreateReimbursementRequest _request = new();
    private List<ExpenseDraft> _expenses = [new()];
    private bool _showCreate;
    private bool _busy;
    private string? _error;
    private string? _message;

    protected override async Task OnInitializedAsync() { await LoadAsync(); await LoadPaymentMethodsAsync(); }
    private async Task LoadAsync()
    {
        _busy = true; _error = null;
        var result = await Api.GetMyReimbursementsAsync();
        _busy = false;
        if (result.IsSuccess) _items = result.Value?.ToList() ?? []; else { _items = []; _error = result.Error; }
    }
    private async Task LoadPaymentMethodsAsync()
    {
        var result = await MemberApi.GetPaymentInformationAsync();
        if (result.IsSuccess)
        {
            _paymentMethods = result.Value?.ToList() ?? [];
            if (_request.PaymentInformationId == Guid.Empty || _paymentMethods.All(item => item.Id != _request.PaymentInformationId)) _request.PaymentInformationId = _paymentMethods.FirstOrDefault()?.Id ?? Guid.Empty;
        }
        else { _paymentMethods = []; _error = result.Error; }
    }
    private void ToggleCreate() { _showCreate = !_showCreate; _error = null; _message = null; }
    private void AddExpense() => _expenses.Add(new ExpenseDraft());
    private void RemoveExpense(ExpenseDraft draft) => _expenses.Remove(draft);
    private async Task SubmitAsync()
    {
        _error = null; _message = null;
        var expenseWithoutReceiptIndex = _expenses.FindIndex(draft => draft.Receipts.Count == 0);
        if (expenseWithoutReceiptIndex >= 0)
        {
            _error = Text["Reimbursements_ReceiptRequired", expenseWithoutReceiptIndex + 1];
            return;
        }

        var files = new List<ReceiptUploadFile>();
        _request.Expenses = [];
        foreach (var draft in _expenses)
        {
            var indexes = Enumerable.Range(files.Count, draft.Receipts.Count).ToList();
            files.AddRange(draft.Receipts);
            _request.Expenses.Add(new CreateExpenseItemRequest { Description = draft.Description, Amount = draft.Amount, IncurredOn = draft.IncurredOn, ReceiptIndexes = indexes });
        }
        _busy = true;
        var result = await Api.CreateReimbursementAsync(_request, files);
        _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        _request = new CreateReimbursementRequest { PaymentInformationId = _paymentMethods.FirstOrDefault()?.Id ?? Guid.Empty };
        _expenses = [new ExpenseDraft()]; _showCreate = false; _message = Text["Reimbursements_Submitted"];
        await LoadAsync();
    }
    private async Task DownloadReceiptAsync(ReceiptDto receipt, bool administrative)
    {
        var result = await Api.DownloadReceiptAsync(receipt.Id, administrative);
        if (!result.IsSuccess) { _error = result.Error; return; }
        await Js.InvokeVoidAsync("akGaming.downloadFile", receipt.FileName, receipt.ContentType, Convert.ToBase64String(result.Value!));
    }

    private async Task CancelAsync(Guid id)
    {
        var confirmed = await Js.InvokeAsync<bool>("confirm", "Cancel this reimbursement? This cannot be undone.");
        if (!confirmed) return;
        _busy = true; _error = null; _message = null;
        var result = await Api.CancelMyReimbursementAsync(id);
        _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        var index = _items?.FindIndex(item => item.Id == id) ?? -1;
        if (index >= 0) _items![index] = result.Value!;
        _message = "Reimbursement cancelled.";
    }
}
