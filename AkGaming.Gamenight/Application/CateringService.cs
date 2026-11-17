using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Domain;

namespace AkGaming.Gamenight.Application;

public interface ICateringService
{
    Task<CateringMenu> MenuAsync(Guid eventId, Actor actor, CancellationToken ct);
    Task<CateringMenu> SaveMenuAsync(CateringMenu menu, Actor actor, CancellationToken ct);
    Task<List<CateringView>> OrdersAsync(Actor actor, bool mine, CancellationToken ct);
    Task<CateringView> PlaceAsync(Guid registrationId, PlaceCateringOrder request, Actor actor, CancellationToken ct);
    Task<CateringView> ActionAsync(Guid registrationId, CateringAction request, Actor actor, CancellationToken ct);
    Task SettleAsync(Guid registrationId, SettleVisitor request, Actor actor, CancellationToken ct);
}

public sealed class CateringService(IGamenightStore store, IGamenightService registrations, TimeProvider clock) : ICateringService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static void Require(bool condition, int status, string message)
    {
        if (!condition) throw new GamenightException(status, message);
    }
    private static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        Require(Validator.TryValidateObject(value, new ValidationContext(value), errors, true), 400,
            string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private async Task<EventSettings> EventAsync(Guid eventId, bool write, CancellationToken ct)
    {
        var selection = await store.SelectionAsync(ct);
        Require(selection.EventId == eventId, 409, "Die aktive Game Night wurde geändert. Bitte neu laden.");
        var entity = await store.EventAsync(eventId, ct);
        Require(entity is not null, 404, "Veranstaltung nicht gefunden.");
        if (write) selection.Version = Guid.NewGuid();
        return JsonSerializer.Deserialize<EventSettings>(entity!.SettingsJson)!;
    }
    public async Task<CateringMenu> MenuAsync(Guid eventId, Actor actor, CancellationToken ct)
    {
        Require(actor.Id is not null, 401, "Bitte anmelden.");
        // Event administrators can prepare menus for an inactive event.
        Require(actor.Has(Permissions.Events) || (await store.SelectionAsync(ct)).EventId == eventId, 403, "Keine Berechtigung.");
        Require(await store.EventAsync(eventId, ct) is not null, 404, "Veranstaltung nicht gefunden.");
        var settings = await store.CateringSettingsAsync(eventId, ct);
        if (settings is null) return new CateringMenu { EventId = eventId };
        var menu = JsonSerializer.Deserialize<CateringMenu>(settings.MenuJson)!;
        menu.Version = settings.Version;
        return menu;
    }
    public async Task<CateringMenu> SaveMenuAsync(CateringMenu menu, Actor actor, CancellationToken ct)
    {
        Require(actor.Has(Permissions.Events), 403, "Keine Berechtigung zur Menüverwaltung.");
        Validate(menu);
        Require(menu.Categories.Count <= 200 && menu.Items.Count <= 1000, 400, "Das Menü ist zu groß.");
        Require(menu.Categories.Select(c => c.Id).Distinct().Count() == menu.Categories.Count
            && menu.Items.Select(i => i.Id).Distinct().Count() == menu.Items.Count, 400, "Doppelte Menüeinträge.");
        var categories = menu.Categories.ToDictionary(c => c.Id);
        foreach (var category in menu.Categories)
        {
            Validate(category);
            Require(category.Id != Guid.Empty, 400, "Ungültige Kategorie.");
            var visited = new HashSet<Guid> { category.Id };
            var parent = category.ParentId;
            while (parent is { } id)
            {
                Require(categories.ContainsKey(id) && visited.Add(id), 400, "Kategorien dürfen keine Zyklen oder unbekannte Eltern enthalten.");
                parent = categories[id].ParentId;
            }
        }
        foreach (var item in menu.Items)
        {
            Validate(item);
            Require(item.Id != Guid.Empty && categories.ContainsKey(item.CategoryId) && CateringIcons.All.Contains(item.Icon), 400, "Ungültige Kategorie oder Symbol.");
        }
        Require(await store.EventAsync(menu.EventId, ct) is not null, 404, "Veranstaltung nicht gefunden.");
        var settings = await store.CateringSettingsAsync(menu.EventId, ct);
        if (settings is null)
        {
            Require(menu.Version == Guid.Empty, 409, "Menü zwischenzeitlich geändert.");
            settings = new CateringSettings { EventId = menu.EventId };
            store.Add(settings);
        }
        else Require(settings.Version == menu.Version, 409, "Menü zwischenzeitlich geändert. Bitte neu laden.");
        var selection = await store.SelectionAsync(ct);
        if (selection.EventId == menu.EventId) selection.Version = Guid.NewGuid();
        settings.MenuJson = JsonSerializer.Serialize(menu);
        settings.Version = Guid.NewGuid();
        Audit(menu.EventId, null, actor, "catering.menu.saved");
        await store.SaveAsync(ct);
        menu.Version = settings.Version;
        return menu;
    }
    private static List<CateringLine> Lines(CateringAccount? account) =>
        account is null ? [] : JsonSerializer.Deserialize<List<CateringLine>>(account.LinesJson)!;
    private static CateringView View(Registration registration, EventSettings settings, CateringAccount? account)
    {
        var person = JsonSerializer.Deserialize<SignupForm>(registration.AnswersJson)!;
        return new(registration.Id, account?.Version ?? Guid.Empty, person.FirstName + " " + person.LastName,
            person.Email, registration.Cancelled, registration.CreatedAt <= settings.EarlyBirdDeadline,
            account?.EligibilityOverride, Lines(account));
    }
    public async Task<List<CateringView>> OrdersAsync(Actor actor, bool mine, CancellationToken ct)
    {
        Require(actor.Id is not null && (mine || actor.Has(Permissions.Read)), 403, "Keine Berechtigung.");
        if (mine) await registrations.MineAsync(actor, ct); // Claims matching guest registrations by verified email.
        var selection = await store.SelectionAsync(ct);
        if (selection.EventId is not { } eventId) return [];
        var settings = await EventAsync(eventId, false, ct);
        var rows = await store.RegistrationsAsync(eventId, ct);
        var result = new List<CateringView>();
        foreach (var row in rows.Where(r => !mine || r.OwnerId == actor.Id))
            result.Add(View(row, settings, await store.CateringAccountAsync(row.Id, ct)));
        return result.OrderBy(v => v.Person).ToList();
    }
    private async Task<(Registration Registration, EventSettings Event, CateringAccount Account)> LoadAsync(
        Guid registrationId, Guid eventId, Guid version, Actor actor, bool staffOnly, CancellationToken ct)
    {
        Require(actor.Id is not null, 401, "Bitte anmelden.");
        var settings = await EventAsync(eventId, true, ct);
        var registration = await store.RegistrationAsync(registrationId, ct);
        Require(registration is not null && registration.EventId == eventId, 404, "Anmeldung nicht gefunden.");
        Require(actor.Has(Permissions.Frontdesk) || (!staffOnly && registration!.OwnerId == actor.Id), 403, "Keine Berechtigung.");
        var account = await store.CateringAccountAsync(registrationId, ct);
        Require((account?.Version ?? Guid.Empty) == version, 409, "Bestellungen zwischenzeitlich geändert. Bitte neu laden.");
        if (account is null)
        {
            account = new CateringAccount { RegistrationId = registrationId, Version = Guid.Empty };
        }
        return (registration!, settings, account);
    }
    private static void AllocateDiscount(List<CateringLine> lines, bool eligible)
    {
        foreach (var line in lines.Where(l => !l.Paid)) { line.DiscountCents = 0; line.DiscountApplied = false; }
        if (!eligible || lines.Any(l => l.Paid && l.DiscountApplied)) return;
        var best = lines.Where(l => !l.Paid && l.PossibleDiscountCents > 0)
            .OrderByDescending(l => l.PossibleDiscountCents).ThenBy(l => l.OrderedAt).ThenBy(l => l.Id).FirstOrDefault();
        if (best is not null) { best.DiscountCents = best.PossibleDiscountCents; best.DiscountApplied = true; }
    }
    private void Audit(Guid eventId, Guid? registrationId, Actor actor, string action)
    {
        store.Add(new AuditEntry { EventId = eventId, RegistrationId = registrationId, Actor = actor.Id!, Action = action, At = Now });
    }
    private void Store(CateringAccount account, List<CateringLine> lines)
    {
        if (account.Version == Guid.Empty) store.Add(account);
        account.LinesJson = JsonSerializer.Serialize(lines);
        account.Version = Guid.NewGuid();
    }
    public async Task<CateringView> PlaceAsync(Guid registrationId, PlaceCateringOrder request, Actor actor, CancellationToken ct)
    {
        var (registration, settings, account) = await LoadAsync(registrationId, request.EventId, request.Version, actor, false, ct);
        Require(!registration.Cancelled, 409, "Diese Anmeldung ist storniert.");
        var menu = await MenuAsync(request.EventId, actor, ct);
        Require(menu.Version == request.MenuVersion, 409, "Das Menü wurde geändert. Bitte neu laden.");
        Require(menu.OrdersOpen || actor.Has(Permissions.Frontdesk) && request.AcknowledgeLocked,
            409, "Bestellungen sind gesperrt. Frontdesk muss die Warnung bestätigen.");
        Require(request.Items is { Count: > 0 and <= 100 } && request.Items.All(i => i.Quantity is > 0 and <= 100), 400, "Ungültige Bestellmengen.");
        Require(request.Items.Select(i => i.ItemId).Distinct().Count() == request.Items.Count, 400, "Doppelte Artikel.");
        var lines = Lines(account);
        Require(lines.Count + request.Items.Count <= 1000, 400, "Zu viele Bestellpositionen.");
        foreach (var quantity in request.Items)
        {
            var item = menu.Items.SingleOrDefault(i => i.Id == quantity.ItemId && i.Available);
            Require(item is not null, 409, "Ein Artikel ist nicht mehr verfügbar.");
            lines.Add(new CateringLine
            {
                ItemId = item!.Id, ItemName = item.Name, CategoryName = CategoryPath(menu, item.CategoryId), Icon = item.Icon,
                UnitPriceCents = item.PriceCents, Quantity = quantity.Quantity, OrderedAt = Now,
                PossibleDiscountCents = item.DiscountEligible ? Math.Min(item.PriceCents, item.DiscountOverrideCents ?? menu.DefaultDiscountCents) : 0
            });
        }
        Require(lines.Sum(l => (long)l.UnitPriceCents * l.Quantity) <= 100_000_000, 400, "Maximaler Bestellwert überschritten.");
        AllocateDiscount(lines, account.EligibilityOverride ?? registration.CreatedAt <= settings.EarlyBirdDeadline);
        Store(account, lines);
        Audit(request.EventId, registrationId, actor, menu.OrdersOpen ? "catering.ordered" : "catering.ordered.lock-override");
        await store.SaveAsync(ct);
        return View(registration, settings, account);
    }
    public static string CategoryPath(CateringMenu menu, Guid categoryId)
    {
        var parts = new List<string>();
        var current = menu.Categories.Single(c => c.Id == categoryId);
        while (true)
        {
            parts.Insert(0, current.Name);
            if (current.ParentId is not { } parent) break;
            current = menu.Categories.Single(c => c.Id == parent);
        }
        return string.Join(" / ", parts);
    }
    public async Task<CateringView> ActionAsync(Guid registrationId, CateringAction request, Actor actor, CancellationToken ct)
    {
        var staffOnly = request.Action != "cancel";
        var (registration, settings, account) = await LoadAsync(registrationId, request.EventId, request.Version, actor, staffOnly, ct);
        var lines = Lines(account);
        var line = lines.SingleOrDefault(l => l.Id == request.LineId);
        switch (request.Action)
        {
            case "pay":
                Require(!registration.Cancelled && lines.Any(l => !l.Paid), 409, "Keine offenen Bestellungen.");
                foreach (var unpaid in lines.Where(l => !l.Paid)) unpaid.Paid = true;
                break;
            case "collect":
                Require(line is not null && line.Paid, 409, "Abholung nur nach Bezahlung möglich.");
                Require(request.Quantity is > 0 && line!.CollectedQuantity + request.Quantity <= line.Quantity, 400, "Ungültige Abholmenge.");
                line!.CollectedQuantity += request.Quantity!.Value;
                break;
            case "uncollect":
                Require(line is not null && line.CollectedQuantity > 0 && !string.IsNullOrWhiteSpace(request.Reason), 400, "Bitte einen Grund für die Korrektur angeben.");
                line!.CollectedQuantity = 0;
                break;
            case "eligibility":
                Require(!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Length <= 500, 400, "Bitte einen Grund für die Rabattkorrektur angeben.");
                account.EligibilityOverride = request.EligibilityOverride;
                break;
            case "cancel":
                Require(line is not null && !line.Paid && line.CollectedQuantity == 0, 409, "Nur unbezahlte, nicht abgeholte Positionen können storniert werden.");
                var menu = await MenuAsync(request.EventId, actor, ct);
                Require(actor.Has(Permissions.Frontdesk) || menu.OrdersOpen, 409, "Bestellungen sind gesperrt.");
                lines.Remove(line!);
                break;
            default: throw new GamenightException(400, "Unbekannte Catering-Aktion.");
        }
        AllocateDiscount(lines, account.EligibilityOverride ?? registration.CreatedAt <= settings.EarlyBirdDeadline);
        Store(account, lines);
        Audit(request.EventId, registrationId, actor, "catering." + request.Action + (request.Action == "eligibility" ? $"={request.EligibilityOverride?.ToString() ?? "automatic"}" : "") + (request.Reason is null ? "" : ": " + request.Reason));
        await store.SaveAsync(ct);
        return View(registration, settings, account);
    }
    public async Task SettleAsync(Guid registrationId, SettleVisitor request, Actor actor, CancellationToken ct)
    {
        var (registration, settings, account) = await LoadAsync(registrationId, request.EventId, request.CateringVersion, actor, true, ct);
        Require(!registration.Cancelled && registration.Version == request.RegistrationVersion, 409, "Anmeldung zwischenzeitlich geändert.");
        var admission = (await registrations.DeskAsync(actor, ct)).Single(r => r.Id == registrationId);
        Require(admission.PriceCents is not null, 409, "Eintrittspreis muss zuerst geklärt werden.");
        Require(admission.PriceCents == request.ExpectedAdmissionCents, 409, "Der Eintrittspreis wurde geändert. Bitte neu laden und den Betrag prüfen.");
        // Use the same unit of work: admission and catering commit together.
        if (!registration.Paid && admission.PriceCents > 0)
        {
            registration.Paid = true;
            registration.SettledPriceCents = admission.PriceCents;
            registration.SettledAdmission = admission.Admission;
            registration.Version = Guid.NewGuid();
            Audit(request.EventId, registrationId, actor, "registration.pay");
        }
        var lines = Lines(account);
        foreach (var line in lines) line.Paid = true;
        Store(account, lines);
        Audit(request.EventId, registrationId, actor, "catering.visitor.settled");
        await store.SaveAsync(ct);
    }
}
