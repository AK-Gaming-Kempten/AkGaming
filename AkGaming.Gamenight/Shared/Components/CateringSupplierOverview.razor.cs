using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringSupplierOverview
{
    [Parameter] public List<CateringView> Accounts { get; set; } = [];
}
