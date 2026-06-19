using System.Text.Json;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class ApplyForMembershipPanel : ComponentBase {
    [Inject] 
    private MemberManagementApiClient MemberApi { get; set; } = default!;
    [Parameter] 
    public Guid UserGuid { get; set; } = Guid.Empty;

    [Parameter]
    public MemberDto Member { get; set; } = default!;

    [Parameter]
    public EventCallback<MemberDto> OnSubmitted { get; set; }
    
    private readonly MembershipApplicationRequestDto _application = new();
    private MemberDto _profile = new();
    private string? _createError;

    protected override void OnParametersSet() {
        _profile = Clone(Member);
        _profile.Address ??= new AddressDto();
    }

    private async Task ApplyForMembershipAsync() {
        _createError = null;
        if(UserGuid == Guid.Empty) {
            _createError = "Can not apply for membership without a valid user ID!";
            return;
        }
        if (!_application.PrivacyPolicyAccepted) {
            _createError = "Please accept the privacy policy.";
            return;
        }
        
        _application.IssuingUserId = UserGuid;
        _application.MemberCreationInfo = new MemberCreationDto {
            FirstName = _profile.FirstName,
            LastName = _profile.LastName,
            Email = _profile.Email,
            Phone = _profile.Phone,
            DiscordUserName = _profile.DiscordUserName,
            BirthDate = _profile.BirthDate,
            Address = new AddressDto(_profile.Address.Street, _profile.Address.ZipCode, _profile.Address.City, _profile.Address.Country)
        };
        
        try {
            var profileResult = await MemberApi.UpdateMemberAsync(_profile);
            if (!profileResult.IsSuccess) {
                _createError = profileResult.Error ?? "Profile could not be updated.";
                return;
            }

            var response = await MemberApi.ApplyForMembershipAsync(_application);
            if (response.IsSuccess) {
                await OnSubmitted.InvokeAsync(_profile);
            }
            else {
                _createError = response.Error;
            }
        }
        catch (Exception ex) {
            _createError = ex.Message;
        }
    }

    private static MemberDto Clone(MemberDto source) =>
        JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(source)) ?? new MemberDto();
}
