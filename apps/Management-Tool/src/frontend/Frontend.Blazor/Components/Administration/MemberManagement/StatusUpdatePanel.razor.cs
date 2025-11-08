using System.ComponentModel.DataAnnotations;
using Frontend.Blazor.ApiClients;
using Microsoft.AspNetCore.Components;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;

namespace Frontend.Blazor.Components.Administration.MemberManagement;

public partial class StatusUpdatePanel : ComponentBase {
    [Parameter] public MemberDto? Member { get; set; }
    [Parameter] public bool Editable { get; set; }

    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient Api { get; set; } = default!;

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
    }
    
    private async Task InsertStatusChangeEventAsync() {
        var result = await Api.InsertMembershipStatusChangeAsync(Member!.Id, _insertModel);
        if (!result.IsSuccess) {
            Console.WriteLine($"Status update failed: {result.Error}");
            return;
        }
        _insertModel = new();
        await LoadStatusHistoryAsync();
    }
}