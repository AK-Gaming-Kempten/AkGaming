using AkGaming.Management.Frontend.ApiClients;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public partial class ExpenseDraftEditor : ComponentBase
{
    private const long MaximumReceiptSize = 10 * 1024 * 1024;

    [Parameter, EditorRequired] public required ExpenseDraft Draft { get; set; }
    [Parameter] public int Number { get; set; }
    [Parameter] public bool CanRemove { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }

    private string? _receiptError;
    private bool _loadingReceipts;

    private async Task FilesSelected(InputFileChangeEventArgs args)
    {
        _receiptError = null;
        _loadingReceipts = true;

        try
        {
            foreach (var browserFile in args.GetMultipleFiles(10))
            {
                await using var source = browserFile.OpenReadStream(MaximumReceiptSize);
                using var target = new MemoryStream();
                await source.CopyToAsync(target);
                Draft.Receipts.Add(new ReceiptUploadFile(
                    browserFile.Name,
                    browserFile.ContentType,
                    target.ToArray()));
            }
        }
        catch (IOException)
        {
            _receiptError = Text["Expense_FileReadFailed"];
        }
        finally
        {
            _loadingReceipts = false;
        }
    }

    private void RemoveReceipt(ReceiptUploadFile receipt)
    {
        Draft.Receipts.Remove(receipt);
        _receiptError = null;
    }

    private static string FormatSize(long size) => size >= 1024 * 1024 ? $"{size / 1024d / 1024d:N1} MB" : $"{Math.Max(1, size / 1024d):N0} KB";
}
