using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringOrderTree
{
    [Parameter, EditorRequired] public CateringMenu Menu { get; set; } = default!;
    [Parameter] public Guid? ParentId { get; set; }
    [Parameter] public Dictionary<Guid, int> Quantities { get; set; } = [];
    [Parameter] public EventCallback<CateringQuantity> QuantityChanged { get; set; }
}
