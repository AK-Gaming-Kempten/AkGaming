using Microsoft.AspNetCore.Components;
namespace AkGaming.Management.Frontend.Components.Administration.Invoices;
public partial class InvoicePresetsPage : ComponentBase
{
    protected enum PresetTab { Parties, PaymentTerms, BankAccounts, LineItems, LineItemCollections }
    private PresetTab _activeTab;
    private static string GetTabLabel(PresetTab tab) => tab switch { PresetTab.Parties => "Parties", PresetTab.PaymentTerms => "Payment terms", PresetTab.BankAccounts => "Bank accounts", PresetTab.LineItems => "Line items", PresetTab.LineItemCollections => "Item collections", _ => tab.ToString() };
}
