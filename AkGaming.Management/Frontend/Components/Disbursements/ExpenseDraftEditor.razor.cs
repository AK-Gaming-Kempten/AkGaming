using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class ExpenseDraftEditor : ComponentBase
{
    [Parameter, EditorRequired] public required ExpenseDraft Draft { get; set; }
    [Parameter] public int Number { get; set; }
    [Parameter] public bool CanRemove { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }

    private void FilesSelected(InputFileChangeEventArgs args) => Draft.Receipts = args.GetMultipleFiles(10).ToList();
    private static string FormatSize(long size) => size >= 1024 * 1024 ? $"{size / 1024d / 1024d:N1} MB" : $"{Math.Max(1, size / 1024d):N0} KB";
}
