using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.MemberManagement.Requests;

public partial class ApplicationRequestManagementPanel : ComponentBase {
    
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    
    private List<MembershipApplicationRequestDto>? _requests;
    
    private MembershipApplicationRequestDto? _selectedRequest = null;

    protected override async Task OnInitializedAsync() {
        await LoadLinkingRequestsAsync();
    }

    private async Task LoadLinkingRequestsAsync() {
        try {
            var result = await MemberApi.GetAllMembershipApplicationRequestsAsync();
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
    
    private void SelectRequest(MembershipApplicationRequestDto request) {
        _selectedRequest = request;
        StateHasChanged();
    }
    
    private void SelectRequest(Guid id) {
        _selectedRequest = _requests?.FirstOrDefault(r => r.Id == id);
        StateHasChanged();
    }
    
    private async Task Reload(MembershipApplicationRequestDto request) {
        await LoadLinkingRequestsAsync();
        StateHasChanged();
        SelectRequest(request.Id);
    }
    
    private void OpenFilters() {
        // TODO
    }
}