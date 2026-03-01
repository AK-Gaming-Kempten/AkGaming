using System.ComponentModel.DataAnnotations;
using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class StatusUpdatePanel : ComponentBase {
    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient Api { get; set; } = default!;
    [Parameter] public MemberDto? Member { get; set; }
    [Parameter] public bool Editable { get; set; }

    [Parameter] public EventCallback<MemberDto> OnStatusUpdated { get; set; }

    private enum StatusTab {
        SetStatus,
        InsertEvent
    }

    private StatusTab _activeTab = StatusTab.SetStatus;

    private IEnumerable<MembershipStatusChangeEventDto>? _statusChanges;
    
    private MembershipStatus _updateStatus = MembershipStatus.None;
    private MembershipStatusChangeEventDto _insertModel = new();

    protected override async Task OnParametersSetAsync() {
        if (Member != null) {
            await LoadStatusHistoryAsync();
        }
    }

    private async Task LoadStatusHistoryAsync() {
        var result = await Api.GetMembershipStatusChangesAsync(Member!.Id);
        if (!result.IsSuccess) {
            _statusChanges = [];
            return;
        }
        _statusChanges = result.Value;
    }

    private async Task UpdateStatusAsync() {
        if (_updateStatus == MembershipStatus.None)
            return;
        var result = await Api.UpdateMembershipStatusAsync(Member!.Id, _updateStatus);
        if (!result.IsSuccess) {
            Console.WriteLine($"Status update failed: {result.Error}");
            return;
        }
        _updateStatus = MembershipStatus.None;
        await LoadStatusHistoryAsync();
        await OnStatusUpdated.InvokeAsync(Member);
    }
    
    private async Task InsertStatusChangeEventAsync() {
        var result = await Api.InsertMembershipStatusChangeAsync(Member!.Id, _insertModel);
        if (!result.IsSuccess) {
            Console.WriteLine($"Status update failed: {result.Error}");
            return;
        }
        _insertModel = new();
        await LoadStatusHistoryAsync();
        await OnStatusUpdated.InvokeAsync(Member);
    }
}