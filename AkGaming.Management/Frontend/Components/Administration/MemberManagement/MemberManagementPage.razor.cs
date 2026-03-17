using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement;

public partial class MemberManagementPage : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MemberDto>? _members;

    private string? _createError;

    private MemberDto? _selectedMember = null;
    private bool _isMobileDetailOpen;

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
        _isMobileDetailOpen = true;
        StateHasChanged();
    }
    
    private void SelectMember(Guid id) {
        _selectedMember = _members?.FirstOrDefault(m => m.Id == id);
        _isMobileDetailOpen = _selectedMember is not null;
        StateHasChanged();
    }
    
    private async Task Reload(MemberDto member) {
        await LoadMembersAsync();
        StateHasChanged();
        SelectMember(member.Id);
    }
    
    private async Task CreateMember() {
        var creationResult = await MemberApi.CreateMemberAsync(new MemberCreationDto() { Address = new AddressDto() });
        if (!creationResult.IsSuccess) {
            _createError = creationResult.Error;
            return;
        }
        var newMemberId = creationResult.Value;
        await LoadMembersAsync();
        SelectMember(newMemberId);
    }
    
    private void OpenFilters() {
        // TODO
    }

    private void ShowListMobile() {
        _isMobileDetailOpen = false;
        _selectedMember = null;
    }
} 
