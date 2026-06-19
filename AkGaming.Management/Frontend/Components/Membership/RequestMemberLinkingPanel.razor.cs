using System.Text.Json;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class RequestMemberLinkingPanel : ComponentBase {
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    [Parameter] 
    public Guid UserGuid { get; set; } = Guid.Empty;

    [Parameter]
    public MemberDto Member { get; set; } = default!;

    [Parameter]
    public EventCallback<MemberDto> OnSubmitted { get; set; }

    private MemberLinkingRequestDto _request = new();
    private MemberDto _profile = new();
    private string? _requestError;

    protected override void OnParametersSet() {
        _profile = Clone(Member);
        _profile.Address ??= new AddressDto();
    }

    private async Task RequestMemberLinkingAsync() {
        _requestError = null;
        if(UserGuid == Guid.Empty) {
            _requestError = "Can not apply for membership without a valid user ID!";
            return;
        }
        if (!_request.PrivacyPolicyAccepted) {
            _requestError = "Please accept the privacy policy.";
            return;
        }
        
        _request.IssuingUserId = UserGuid;
        _request.FirstName = _profile.FirstName ?? string.Empty;
        _request.LastName = _profile.LastName ?? string.Empty;
        _request.Email = _profile.Email ?? string.Empty;
        _request.DiscordUserName = _profile.DiscordUserName ?? string.Empty;
        
        try {
            var profileResult = await MemberApi.UpdateMemberAsync(_profile);
            if (!profileResult.IsSuccess) {
                _requestError = profileResult.Error ?? "Profile could not be updated.";
                return;
            }

            var response = await MemberApi.SendMemberLinkingRequestAsync(_request);
            if (response.IsSuccess) {
                await OnSubmitted.InvokeAsync(_profile);
            }
            else {
                _requestError = response.Error;
            }
        }
        catch (Exception ex) {
            _requestError = ex.Message;
        }
    }

    private static MemberDto Clone(MemberDto source) =>
        JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(source)) ?? new MemberDto();
}
