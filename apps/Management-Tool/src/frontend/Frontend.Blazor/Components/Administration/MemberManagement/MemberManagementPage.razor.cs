using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration.MemberManagement;

public partial class MemberManagementPage : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MemberDto>? _members;
    private MemberCreationDto _newMember = new MemberCreationDto() { Address = new AddressDto() };
    private string? _createError;

    private MemberDto? _selectedMember = null;

    protected override async Task OnInitializedAsync() {
        await LoadMembersAsync();
    }

    private async Task LoadMembersAsync() {
        try {
            var result = await MemberApi.GetAllMembersAsync();
            if(result.IsSuccess)
                _members = result.Value?.ToList();
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
}