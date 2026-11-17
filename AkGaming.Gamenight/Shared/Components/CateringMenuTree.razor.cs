using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Components;
namespace AkGaming.Gamenight.Shared.Components;
public partial class CateringMenuTree
{
    [Parameter, EditorRequired] public CateringMenu Menu { get; set; } = default!;
    [Parameter] public Guid? Selected { get; set; }
    [Parameter] public EventCallback<Guid> SelectedChanged { get; set; }
    [Parameter] public EventCallback<CateringCategory> EditCategory { get; set; }
    [Parameter] public EventCallback<Guid?> CreateCategory { get; set; }
    [Parameter] public EventCallback<Guid> CreateItem { get; set; }
    [Parameter] public EventCallback<Guid> DeleteCategory { get; set; }
    [Parameter] public EventCallback<CateringItem> EditItem { get; set; }
    private Guid? _dragCategory, _dragItem;
    private string? _dropTarget;
    private sealed record Entry(Guid Id, int Depth, CateringCategory? Category = null, CateringItem? Item = null, bool End = false);
    private IEnumerable<Entry> Entries(Guid? parent, int depth)
    {
        foreach (var category in Menu.Categories.Where(c => c.ParentId == parent).OrderBy(c => c.Position))
        {
            yield return new(category.Id, depth, Category: category);
            foreach (var item in Menu.Items.Where(i => i.CategoryId == category.Id).OrderBy(i => i.Position))
                yield return new(item.Id, depth + 1, Item: item);
            foreach (var child in Entries(category.Id, depth + 1)) yield return child;
            yield return new(category.Id, depth + 1, Category: category, End: true);
        }
    }
    private static string Money(int cents) => (cents / 100m).ToString("C", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
    private static string Before(Guid id) => "before-" + id;
    private static string Into(Guid id) => "into-" + id;
    private bool CanNest(Guid? parent)
    {
        for (var current = parent; current is not null; current = Menu.Categories.Single(c => c.Id == current).ParentId)
            if (current == _dragCategory) return false;
        return true;
    }
    private void HoverCategory(Guid id) => _dropTarget = (_dragItem is not null || _dragCategory is not null && CanNest(id)) ? Into(id) : null;
    private void HoverBefore(Entry entry)
    {
        var valid = entry.Category is { } category ? _dragCategory is not null && _dragCategory != entry.Id && CanNest(category.ParentId) : _dragItem is not null && _dragItem != entry.Id;
        _dropTarget = valid ? Before(entry.Id) : null;
    }
    private void HoverRoot() => _dropTarget = _dragCategory is not null ? "root" : null;
    private void StartCategory(Guid id) { _dragCategory = id; _dragItem = null; }
    private void StartItem(Guid id) { _dragItem = id; _dragCategory = null; }
    private void EndDrag() { _dragCategory = null; _dragItem = null; _dropTarget = null; }
    private void DropRoot() { MoveCategory(null, null); EndDrag(); }
    private void DropBefore(Entry entry)
    {
        if (entry.Category is { } category) MoveCategory(category.ParentId, category.Id);
        else if (entry.Item is { } item) MoveItem(item.CategoryId, item.Id);
        EndDrag();
    }
    private void DropInto(Guid id)
    {
        if (_dragCategory is not null) MoveCategory(id, null);
        if (_dragItem is not null) MoveItem(id, null);
        EndDrag();
    }
    private void MoveCategory(Guid? parent, Guid? before)
    {
        if (_dragCategory is not { } id || before == id || !CanNest(parent)) return;
        var category = Menu.Categories.Single(c => c.Id == id);
        var siblings = Menu.Categories.Where(c => c.ParentId == parent && c.Id != id).OrderBy(c => c.Position).ToList();
        category.ParentId = parent;
        siblings.Insert(before is null ? siblings.Count : siblings.FindIndex(c => c.Id == before), category);
        for (var n = 0; n < siblings.Count; n++) siblings[n].Position = n;
    }
    private void MoveItem(Guid categoryId, Guid? before)
    {
        if (_dragItem is not { } id || before == id) return;
        var item = Menu.Items.Single(i => i.Id == id);
        var siblings = Menu.Items.Where(i => i.CategoryId == categoryId && i.Id != id).OrderBy(i => i.Position).ToList();
        item.CategoryId = categoryId;
        siblings.Insert(before is null ? siblings.Count : siblings.FindIndex(i => i.Id == before), item);
        for (var n = 0; n < siblings.Count; n++) siblings[n].Position = n;
    }
}
