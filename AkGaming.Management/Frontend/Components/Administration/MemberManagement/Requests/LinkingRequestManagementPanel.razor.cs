using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class LinkingRequestManagementPanel : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MemberLinkingRequestDto>? _requests;
    
    private MemberLinkingRequestDto? _selectedRequest = null;
    private bool _isMobileDetailOpen;

    protected override async Task OnInitializedAsync() {
        await LoadLinkingRequestsAsync();
    }

    private async Task LoadLinkingRequestsAsync() {
        try {
            var result = await MemberApi.GetAllMemberLinkingRequestAsync();
            if (result.IsSuccess) {
                _requests = result.Value?.ToList();
            }
        }
        catch (Exception ex) {
            Console.WriteLine("Error fetching requests: " + ex);
            _requests = new();
        }
        StateHasChanged();
    }
    
    private void SelectRequest(MemberLinkingRequestDto request) {
        _selectedRequest = request;
        _isMobileDetailOpen = true;
        StateHasChanged();
    }
    
    private void SelectRequest(Guid id) {
        _selectedRequest = _requests?.FirstOrDefault(r => r.Id == id);
        _isMobileDetailOpen = _selectedRequest is not null;
        StateHasChanged();
    }
    
    private async Task Reload(MemberLinkingRequestDto request) {
        await LoadLinkingRequestsAsync();
        StateHasChanged();
        SelectRequest(request.Id);
    }
    
    private void OpenFilters() {
        // TODO
    }

    private void ShowListMobile() {
        _isMobileDetailOpen = false;
        _selectedRequest = null;
    }
} 
