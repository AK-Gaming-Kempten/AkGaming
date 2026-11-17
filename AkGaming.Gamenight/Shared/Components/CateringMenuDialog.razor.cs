using System.Text.Json;
using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringMenuDialog
{
    [Inject] private GamenightClient Client { get; set; } = default!;
    [Parameter] public Guid EventId { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    private CateringMenu? _menu;
    private CateringCategory? _categoryDraft;
    private CateringItem? _itemDraft;
    private Guid? _selected, _deleteCategory;
    private string? _error;
    private bool _busy;
    private decimal DefaultDiscount { get => _menu!.DefaultDiscountCents / 100m; set => _menu!.DefaultDiscountCents = ToCents(value); }
    private decimal ItemPrice { get => _itemDraft!.PriceCents / 100m; set => _itemDraft!.PriceCents = ToCents(value); }
    private decimal? ItemDiscount { get => _itemDraft!.DiscountOverrideCents / 100m; set => _itemDraft!.DiscountOverrideCents = value is null ? null : ToCents(value.Value); }
    private static int ToCents(decimal value) => (int)(Math.Clamp(decimal.Round(value * 100), 0, 100000));
    private static T Copy<T>(T source) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source))!;
    private string DefaultDiscountPlaceholder => $"{(_menu!.DefaultDiscountCents / 100m).ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))}€ (default)";
    private static string IconName(string icon) => icon switch { "pizza" => "Pizza", "doner" => "Döner", "durum" => "Dürüm", _ => "Allgemein" };
    private string Path(Guid id)
    {
        var c = _menu!.Categories.Single(c => c.Id == id);
        return c.ParentId is { } parent ? Path(parent) + " / " + c.Name : c.Name;
    }
    protected override async Task OnInitializedAsync()
    {
        try { _menu = await Client.SendAsync<CateringMenu>(HttpMethod.Get, $"api/catering/menu/{EventId}"); _selected = _menu.Categories.FirstOrDefault()?.Id; }
        catch (HttpRequestException ex) { _error = ex.Message; }
    }
    private void CreateCategory(Guid? parent) => _categoryDraft = new() { ParentId = parent, Position = _menu!.Categories.Count };
    private void EditCategory(CateringCategory category) => _categoryDraft = Copy(category);
    private void SaveCategory()
    {
        _menu!.Categories.RemoveAll(c => c.Id == _categoryDraft!.Id);
        _menu.Categories.Add(_categoryDraft!); _selected = _categoryDraft!.Id; _categoryDraft = null;
    }
    private void CreateItem(Guid categoryId) => _itemDraft = new() { CategoryId = categoryId, Position = _menu!.Items.Count };
    private void EditItem(CateringItem item) => _itemDraft = Copy(item);
    private void SaveItem() { _menu!.Items.RemoveAll(i => i.Id == _itemDraft!.Id); _menu.Items.Add(_itemDraft!); _itemDraft = null; }
    private void CloseEditor() { _categoryDraft = null; _itemDraft = null; _deleteCategory = null; }
    private void DeleteCategory()
    {
        var deleted = new HashSet<Guid> { _deleteCategory!.Value };
        while (true)
        {
            var children = _menu!.Categories.Where(c => c.ParentId is { } parent && deleted.Contains(parent) && !deleted.Contains(c.Id)).Select(c => c.Id).ToList();
            if (children.Count == 0) break;
            deleted.UnionWith(children);
        }
        _menu!.Items.RemoveAll(i => deleted.Contains(i.CategoryId));
        _menu.Categories.RemoveAll(c => deleted.Contains(c.Id));
        if (_selected is { } selected && deleted.Contains(selected)) _selected = null;
        _deleteCategory = null;
    }
    private async Task Close() { if (!_busy) await OnClose.InvokeAsync(); }
    private async Task Save()
    {
        if (_busy || _menu is null) return;
        _busy = true; _error = null;
        try { _menu = await Client.SendAsync<CateringMenu>(HttpMethod.Put, "api/catering/menu", _menu); await OnClose.InvokeAsync(); }
        catch (HttpRequestException ex) { _error = ex.Message; }
        finally { _busy = false; }
    }
}
