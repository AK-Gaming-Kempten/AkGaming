using System.Text.Json;
using AkGaming.Core.Constants;
using AkGaming.Management.Frontend.ApiClients;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Enums;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Membership;

public partial class MemberInfoPanel : ComponentBase {
    [Parameter] public MemberDto Member { get; set; } = default!;
    [Parameter] public EventCallback<MemberDto> OnMemberUpdated { get; set; }

    [CascadingParameter(Name = "MemberManagementApi")]
    public MemberManagementApiClient Api { get; set; } = default!;

    private MemberDto _localMember = new();
    private bool EditMode { get; set; }
    private string? _statusMessage;
    private string? _errorMessage;

    protected override void OnParametersSet() {
        _localMember = Clone(Member);
        _localMember.Address ??= new AddressDto();
        _localMember.StatusChanges ??= [];
    }

    private string DisplayName {
        get {
            var fullName = $"{_localMember.FirstName} {_localMember.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "Member Profile" : fullName;
        }
    }

    private List<MembershipStatusChangeEventDto> HistoryEntries => (_localMember.StatusChanges ?? [])
        .OrderByDescending(x => x.Timestamp)
        .ToList();

    private string StatusIcon => _localMember.Status switch {
        MembershipStatus.Member => "✓",
        MembershipStatus.InTrial => "⌛",
        MembershipStatus.Applicant => "✦",
        _ => "•"
    };

    private string StatusClass => _localMember.Status switch {
        MembershipStatus.Member => "status-member",
        MembershipStatus.InTrial => "status-trial",
        MembershipStatus.Applicant => "status-applicant",
        _ => "status-default"
    };

    private DateTime? MembershipStartDate => (_localMember.StatusChanges ?? [])
        .Where(x => x.NewStatus == MembershipStatus.Member)
        .OrderBy(x => x.Timestamp)
        .Select(x => (DateTime?)x.Timestamp)
        .FirstOrDefault()
        ?? (_localMember.StatusChanges ?? [])
            .OrderBy(x => x.Timestamp)
            .Select(x => (DateTime?)x.Timestamp)
            .FirstOrDefault();

    private int ServiceYears {
        get {
            if (MembershipStartDate is null)
                return 0;

            var start = MembershipStartDate.Value.Date;
            var today = DateTime.UtcNow.Date;
            var years = today.Year - start.Year;
            if (start > today.AddYears(-years))
                years--;

            return Math.Max(years, 0);
        }
    }

    private string ServiceYearsText => MembershipStartDate is null
        ? "Not available yet"
        : ServiceYears == 0 ? "Less than 1 year" : $"{ServiceYears} year{(ServiceYears == 1 ? string.Empty : "s")}";

    private string MembershipStartText => MembershipStartDate is null
        ? "No membership start date recorded."
        : $"Member since {MembershipStartDate.Value:yyyy-MM-dd}";

    private void EnableEditing() {
        _statusMessage = null;
        _errorMessage = null;
        EditMode = true;
    }

    private async Task SaveChangesAsync() {
        _statusMessage = null;
        _errorMessage = null;

        var result = await Api.UpdateMemberAsync(_localMember);
        if (!result.IsSuccess) {
            _errorMessage = result.Error ?? "Profile could not be updated.";
            return;
        }

        Member = Clone(_localMember);
        Member.Address ??= new AddressDto();
        Member.StatusChanges ??= [];
        EditMode = false;
        _statusMessage = "Profile updated.";
        await OnMemberUpdated.InvokeAsync(Member);
    }

    private void CancelChanges() {
        _localMember = Clone(Member);
        _localMember.Address ??= new AddressDto();
        _localMember.StatusChanges ??= [];
        EditMode = false;
        _statusMessage = null;
        _errorMessage = null;
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private string WebsiteUrl => ClubConstants.Urls.Website;
    private string SatzungUrl => ClubConstants.Urls.ArticlesOfAssociation;
    private string DiscordUrl => ClubConstants.Urls.DiscordInvite;

    private static MemberDto Clone(MemberDto source) =>
        JsonSerializer.Deserialize<MemberDto>(JsonSerializer.Serialize(source)) ?? new MemberDto();
}
