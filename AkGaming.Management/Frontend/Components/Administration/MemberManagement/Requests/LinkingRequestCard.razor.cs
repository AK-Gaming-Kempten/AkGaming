using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class LinkingRequestCard : ComponentBase {
    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient MemberApi { get; set; } = default!;
    [Parameter] public MemberLinkingRequestDto? Request { get; set; } = default!;
    [Parameter] public bool IsEditable { get; set; } = false;
    [Parameter] public EventCallback<MemberLinkingRequestDto> OnRequestUpdated { get; set; }
    
    private MemberDto? _selectedMember = default!;
    
    private List<MemberDto>? _members = default!;

    protected override async Task OnInitializedAsync() {
        await LoadMembersAsync();
    }

    private async Task LoadMembersAsync() {
        try {
            var result = await MemberApi.GetAllMembersAsync();
            if (result.IsSuccess) {
                _members = result.Value?.ToList();
            }
        }
        catch (Exception ex) {
            Console.WriteLine("Error fetching members: " + ex);
            _members = new();
        }
        StateHasChanged();
    }
    
    private void SelectMember(MemberDto member) {
        _selectedMember = member;
        StateHasChanged();
    }

    private async Task Link() {
        if (_selectedMember == null || Request == null)
            return;
        var result = await MemberApi.LinkMemberToUserAsync(Request.IssuingUserId, _selectedMember!.Id);
        if (!result.IsSuccess) {
            Console.WriteLine($"Linking failed: {result.Error}");
            return;
        }
        await MemberApi.AcceptMemberLinkingRequestAsync(Request.Id);
        await OnRequestUpdated.InvokeAsync(Request);
    }

    private async Task Reject() {
        if (Request == null)
            return;

        var result = await MemberApi.RejectMemberLinkingRequestAsync(Request.Id);
        if (!result.IsSuccess) {
            Console.WriteLine($"Rejecting linking request failed: {result.Error}");
            return;
        }

        await OnRequestUpdated.InvokeAsync(Request);
    }
}
