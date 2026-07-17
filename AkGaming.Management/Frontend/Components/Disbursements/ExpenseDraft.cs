using Microsoft.AspNetCore.Components.Forms;

namespace AkGaming.Management.Frontend.Components.Disbursements;

public sealed class ExpenseDraft
{
    public Guid Key { get; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public List<IBrowserFile> Receipts { get; set; } = [];
}
