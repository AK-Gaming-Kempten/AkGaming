using System.Security.Claims;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using AkGaming.Management.Modules.Disbursements.Contracts.Enums;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class AllocationClaimPage : ComponentBase
{
    [Parameter] public Guid Token { get; set; }
    [Inject] private DisbursementsApiClient Api { get; set; } = null!;
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    private AllocationDto? _allocation;
    private List<PaymentInformationDto> _paymentMethods = [];
    private CreateAllocationApplicationRequest _application = new();
    private AllocationApplicationDto? _editingApplication;
    private AllocationApplicationDto? _cancellingApplication;
    private Guid _userId;
    private bool _busy;
    private string? _error;
    private string? _message;
    private string? _dialogError;
    private decimal Remaining => _allocation is null ? 0 : _allocation.Amount - _allocation.AppliedAmount;
    private bool HasOwnApplication => _allocation?.Applications.Any(item => item.ApplicantUserId == _userId
        && item.Status is not AllocationApplicationStatus.Rejected
            and not AllocationApplicationStatus.Cancelled) ?? false;

    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        Guid.TryParse(state.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? state.User.FindFirstValue("sub"), out _userId);
        await LoadAsync(); await LoadPaymentMethodsAsync();
    }
    private async Task LoadAsync()
    {
        var result = await Api.GetAllocationAsync(Token);
        if (!result.IsSuccess) { _error = result.Error; return; }
        _allocation = result.Value;
        if (_application.Amount <= 0 || _application.Amount > Remaining) _application.Amount = Remaining;
    }
    private async Task LoadPaymentMethodsAsync()
    {
        var result = await MemberApi.GetPaymentInformationAsync();
        if (!result.IsSuccess) { _paymentMethods = []; _error = result.Error; return; }
        _paymentMethods = result.Value?.ToList() ?? [];
        if (_paymentMethods.All(item => item.Id != _application.PaymentInformationId)) _application.PaymentInformationId = _paymentMethods.FirstOrDefault()?.Id ?? Guid.Empty;
    }
    private async Task ApplyAsync()
    {
        _busy = true; _error = null; _message = null; var result = await Api.ApplyAsync(Token, _application); _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        _message = "Application submitted."; await LoadAsync();
    }
    private async Task DecideAsync(Guid applicationId, bool isApproved)
    {
        _busy = true; _error = null; var result = await Api.DecideAsync(Token, applicationId, new DecideAllocationApplicationRequest { IsApproved = isApproved }); _busy = false;
        if (!result.IsSuccess) { _error = result.Error; return; }
        _message = isApproved ? "Approval recorded." : "Objection recorded."; await LoadAsync();
    }

    private void OpenEditDialog(AllocationApplicationDto application)
    {
        _editingApplication = application;
        _cancellingApplication = null;
        _dialogError = null;
    }

    private void OpenCancelDialog(AllocationApplicationDto application)
    {
        _cancellingApplication = application;
        _editingApplication = null;
        _dialogError = null;
    }

    private async Task UpdateApplicationAsync(UpdateAllocationApplicationRequest request)
    {
        if (_editingApplication is null)
            return;

        _busy = true;
        _dialogError = null;
        var result = await Api.UpdateMyApplicationAsync(Token, _editingApplication.Id, request);
        _busy = false;
        if (!result.IsSuccess)
        {
            _dialogError = result.Error;
            return;
        }

        _message = Text["Allocation_ClaimUpdated"];
        CloseDialogs();
        await LoadAsync();
    }

    private async Task CancelApplicationAsync()
    {
        if (_cancellingApplication is null)
            return;

        _busy = true;
        _dialogError = null;
        var result = await Api.CancelMyApplicationAsync(Token, _cancellingApplication.Id);
        _busy = false;
        if (!result.IsSuccess)
        {
            _dialogError = result.Error;
            return;
        }

        _message = Text["Allocation_ClaimCancelled"];
        CloseDialogs();
        await LoadAsync();
    }

    private void CloseDialogs()
    {
        if (_busy)
            return;
        _editingApplication = null;
        _cancellingApplication = null;
        _dialogError = null;
    }

    private decimal MaximumEditableAmount(AllocationApplicationDto application)
    {
        return Remaining + application.Amount;
    }

    private static bool CanModify(AllocationApplicationDto application)
    {
        return application.Status is not AllocationApplicationStatus.Paid
            and not AllocationApplicationStatus.Rejected
            and not AllocationApplicationStatus.Cancelled;
    }

    private static bool CanReview(AllocationApplicationDto application)
    {
        return CanModify(application);
    }
}
