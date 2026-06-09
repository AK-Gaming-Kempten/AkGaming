using AkGaming.Management.Modules.InvoiceManagement.Contracts.DTO;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Management.Frontend.Components.Administration.Invoices;

public partial class InvoicePartyFields : ComponentBase
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public InvoicePartyDto Value { get; set; } = new();
    [Parameter] public IReadOnlyList<InvoicePartyPresetDto> Presets { get; set; } = [];

    private Task ApplyPresetAsync(ChangeEventArgs args)
    {
        if (!Guid.TryParse(args.Value?.ToString(), out var presetId))
            return Task.CompletedTask;

        var preset = Presets.FirstOrDefault(candidate => candidate.Id == presetId);
        if (preset is null)
            return Task.CompletedTask;

        Value.Name = preset.Party.Name;
        Value.Street = preset.Party.Street;
        Value.PostalCode = preset.Party.PostalCode;
        Value.City = preset.Party.City;
        Value.Country = preset.Party.Country;
        return InvokeAsync(StateHasChanged);
    }
}
