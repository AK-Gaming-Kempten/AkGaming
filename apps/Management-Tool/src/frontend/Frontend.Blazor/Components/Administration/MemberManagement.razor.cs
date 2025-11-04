using Frontend.Blazor.ApiClients;
using MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace Frontend.Blazor.Components.Administration;

public partial class MemberManagement : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MemberDto>? _members;
    private MemberCreationDto _newMember = new MemberCreationDto() { Address = new AddressDto() };
    private string? _createError;

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

    private async Task CreateMemberAsync() {
        _createError = null;

        try {
            var response = await MemberApi.CreateMemberAsync(_newMember);
        }
        catch (Exception ex) {
            _createError = ex.Message;
        }
    }
}