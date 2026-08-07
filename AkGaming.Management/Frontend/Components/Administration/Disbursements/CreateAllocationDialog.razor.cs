using AkGaming.Management.Modules.Disbursements.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Disbursements;

public partial class CreateAllocationDialog : ComponentBase
{
    private const int DiscordGuildVoiceChannelType = 2;
    private const int DiscordGuildStageChannelType = 13;

    [Parameter, EditorRequired]
    public required DiscordGuildCatalogDto Catalog { get; set; }

    [Parameter]
    public AllocationDto? Allocation { get; set; }

    [Parameter]
    public bool CatalogReady { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public EventCallback<SaveAllocationRequest> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private SaveAllocationRequest _model = new();
    private bool _initialized;

    protected override void OnParametersSet()
    {
        if (_initialized)
            return;

        _initialized = true;
        if (Allocation is null)
            return;

        _model = new SaveAllocationRequest
        {
            Name = Allocation.Name,
            Description = Allocation.Description,
            Amount = Allocation.Amount,
            DiscordChannelId = Allocation.DiscordChannelId,
            DiscordChannelName = Allocation.DiscordChannelName,
            DiscordRoleId = Allocation.DiscordRoleId,
            DiscordRoleName = Allocation.DiscordRoleName
        };
    }

    private bool CanSubmit => !string.IsNullOrWhiteSpace(_model.Name)
        && _model.Amount > 0
        && HasCompleteOrEmptyDiscordRouting
        && (CatalogReady || !HasDiscordRouting);

    private bool HasCompleteOrEmptyDiscordRouting =>
        string.IsNullOrWhiteSpace(_model.DiscordChannelId) == string.IsNullOrWhiteSpace(_model.DiscordRoleId);

    private bool HasDiscordRouting => !string.IsNullOrWhiteSpace(_model.DiscordChannelId)
        || !string.IsNullOrWhiteSpace(_model.DiscordRoleId);

    private string ChannelLabel(DiscordChannelDto channel)
    {
        return channel.Type is DiscordGuildVoiceChannelType or DiscordGuildStageChannelType
            ? $"{channel.Name} {Text["Allocation_VoiceChannelSuffix"]}"
            : channel.Name;
    }

    private async Task SubmitAsync()
    {
        if (!string.IsNullOrWhiteSpace(_model.DiscordChannelId)
            && !string.IsNullOrWhiteSpace(_model.DiscordRoleId))
        {
            var channel = Catalog.Channels.FirstOrDefault(item => item.Id == _model.DiscordChannelId);
            var role = Catalog.Roles.FirstOrDefault(item => item.Id == _model.DiscordRoleId);
            _model.DiscordChannelName = channel?.Name ?? _model.DiscordChannelName;
            _model.DiscordRoleName = role?.Name ?? _model.DiscordRoleName;
        }
        else
        {
            _model.DiscordChannelName = string.Empty;
            _model.DiscordRoleName = string.Empty;
        }
        await OnSubmit.InvokeAsync(_model);
    }
}
