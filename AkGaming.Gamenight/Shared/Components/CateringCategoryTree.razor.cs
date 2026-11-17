using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringCategoryTree
{
    [Parameter] public List<CateringCategory> Categories { get; set; } = [];
    [Parameter] public Guid? ParentId { get; set; }
    [Parameter] public Guid? Selected { get; set; }
    [Parameter] public EventCallback<Guid> SelectedChanged { get; set; }
}
