using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class MemberDuesPanel : ComponentBase {
    [Inject] private MemberManagementApiClient MemberApi { get; set; } = default!;

    private bool _loading = true;
    private string? _error;
    private List<MembershipDueDto> _pendingDues = [];
    private List<MembershipDueDto> _paidDues = [];

    protected override async Task OnInitializedAsync() {
        await LoadAsync();
    }

    private async Task LoadAsync() {
        _loading = true;
        _error = null;
        _pendingDues = [];
        _paidDues = [];

        var duesResult = await MemberApi.GetMyDuesAsync();
        if (!duesResult.IsSuccess) {
            _error = duesResult.Error ?? "Failed to load dues.";
            _loading = false;
            return;
        }

        var dues = duesResult.Value ?? [];
        _pendingDues = dues.Where(x => x.Status == MembershipDueStatus.Pending).ToList();
        _paidDues = dues.Where(x => x.Status == MembershipDueStatus.Paid).ToList();

        _loading = false;
    }

    private static string GetSettledAtText(MembershipDueDto due) =>
        due.SettledAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

    private static string GetPaidAmountText(MembershipDueDto due) =>
        $"{(due.PaidAmount?.ToString("0.00") ?? "-")} €";
}
