using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Domain;

namespace AkGaming.Gamenight.Application;

public sealed class GamenightService(IGamenightStore store, IMembershipClient membership, IGuestEmail email, TimeProvider clock) : IGamenightService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static EventSettings Settings(GamenightEvent e)
    {
        var settings = JsonSerializer.Deserialize<EventSettings>(e.SettingsJson)!;
        settings.Version = e.Version;
        return settings;
    }
    private static SignupForm Answers(Registration r) => JsonSerializer.Deserialize<SignupForm>(r.AnswersJson)!;
    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
    private static void Require(bool condition, int status, string message)
    {
        if (!condition) throw new GamenightException(status, message);
    }
    private static void Validate(object value)
    {
        var results = new List<ValidationResult>();
        Require(Validator.TryValidateObject(value, new ValidationContext(value), results, true), 400,
            string.Join(" ", results.Select(r => r.ErrorMessage)));
    }
    private void Audit(Guid eventId, Guid? registration, Actor actor, string action)
    {
        store.Add(new AuditEntry { EventId = eventId, RegistrationId = registration, Actor = actor.Id ?? "guest", Action = action, At = Now });
    }
    private async Task<EventSettings> CurrentAsync(Guid eventId, bool writing, CancellationToken ct)
    {
        var selection = await store.SelectionAsync(ct);
        Require(selection.EventId == eventId, 409, "Die aktive Game Night wurde geändert. Bitte die Seite neu laden.");
        var entity = await store.EventAsync(eventId, ct);
        Require(entity is not null, 404, "Game Night nicht gefunden.");
        // Every operational write participates in the selection's concurrency check.
        if (writing) selection.Version = Guid.NewGuid();
        return Settings(entity!);
    }
    public async Task<ActiveEvent> ActiveAsync(CancellationToken ct)
    {
        var selection = await store.SelectionAsync(ct);
        var entity = selection.EventId is { } id ? await store.EventAsync(id, ct) : null;
        return new(entity is null ? null : Settings(entity), selection.Version);
    }
    public async Task<List<EventSettings>> EventsAsync(Actor actor, CancellationToken ct)
    {
        Require(actor.Has(Permissions.Events), 403, "Keine Berechtigung.");
        return (await store.EventsAsync(ct)).Select(Settings).OrderByDescending(e => e.StartsAt).ToList();
    }
    public async Task<EventSettings> SaveEventAsync(EventSettings settings, Actor actor, CancellationToken ct)
    {
        Require(actor.Has(Permissions.Events), 403, "Keine Berechtigung.");
        Validate(settings);
        settings.StartsAt = Utc(settings.StartsAt);
        settings.EndsAt = Utc(settings.EndsAt);
        settings.SignupDeadline = Utc(settings.SignupDeadline);
        settings.EditDeadline = Utc(settings.EditDeadline);
        settings.EarlyBirdDeadline = Utc(settings.EarlyBirdDeadline);
        settings.FoodDeadline = Utc(settings.FoodDeadline);
        var entity = await store.EventAsync(settings.Id, ct);
        if (entity is null)
        {
            Require(settings.Version == Guid.Empty, 409, "Veranstaltung nicht mehr vorhanden.");
            entity = new GamenightEvent { Id = settings.Id };
            store.Add(entity);
        }
        else Require(entity.Version == settings.Version, 409, "Die Veranstaltung wurde zwischenzeitlich geändert.");
        // Changing active pricing/deadlines must conflict with an in-flight signup/payment.
        var selection = await store.SelectionAsync(ct);
        if (selection.EventId == settings.Id) selection.Version = Guid.NewGuid();
        entity.SettingsJson = JsonSerializer.Serialize(settings);
        entity.Version = Guid.NewGuid();
        Audit(entity.Id, null, actor, "event.saved");
        await store.SaveAsync(ct);
        return Settings(entity);
    }
    public async Task ActivateAsync(ActivateEvent request, Actor actor, CancellationToken ct)
    {
        Require(actor.Has(Permissions.Events), 403, "Keine Berechtigung.");
        var selectedEvent = await store.EventAsync(request.EventId, ct);
        Require(selectedEvent is not null, 404, "Game Night nicht gefunden.");
        var period = Settings(selectedEvent!).MembershipPaymentPeriodId;
        Require(period is not null, 400, "Bitte zuerst den Beitragszeitraum für Mitgliedspreise auswählen.");
        Require((await membership.PeriodsAsync(ct)).Any(p => p.Id == period), 400, "Der Beitragszeitraum ist in Management nicht vorhanden.");
        var selection = await store.SelectionAsync(ct);
        Require(selection.Version == request.SelectionVersion, 409, "Die Auswahl wurde zwischenzeitlich geändert.");
        selection.EventId = request.EventId;
        selection.Version = Guid.NewGuid();
        Audit(request.EventId, null, actor, "event.activated");
        await store.SaveAsync(ct);
    }
    public async Task SubmitAsync(SubmitSignup request, Actor actor, CancellationToken ct)
    {
        request.Form.Email = request.Form.Email.Trim();
        Validate(request.Form);
        var settings = await CurrentAsync(request.EventId, true, ct);
        Require(Now <= settings.SignupDeadline, 409, "Die Anmeldung ist geschlossen.");
        var normalized = Normalize(request.Form.Email);
        var existing = (await store.RegistrationsAsync(request.EventId, ct)).FirstOrDefault(r => r.NormalizedEmail == normalized);
        // Identical public response for new and existing emails; never reveal ownership.
        if (existing is not null) return;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var form = Clean(request.Form);
        if (Now > settings.FoodDeadline)
            Require(form.Attendance == "Karaoke" || form.Meal == "Nichts", 409, "Die Frist für Essenswünsche ist abgelaufen.");
        var registration = new Registration
        {
            EventId = request.EventId, NormalizedEmail = normalized,
            OwnerId = actor.Id is not null && actor.VerifiedEmail is not null && Normalize(actor.VerifiedEmail) == normalized ? actor.Id : null,
            AnswersJson = JsonSerializer.Serialize(form), CreatedAt = Now,
            GuestTokenHash = Hash(token), GuestTokenExpiresAt = settings.EndsAt.AddDays(7)
        };
        store.Add(registration);
        email.Queue(registration, form.Email, token);
        Audit(request.EventId, registration.Id, actor, "registration.created");
        await store.SaveAsync(ct);
    }
    public async Task<List<RegistrationView>> MineAsync(Actor actor, CancellationToken ct)
    {
        Require(actor.Id is not null, 401, "Bitte anmelden.");
        var active = await ActiveAsync(ct);
        if (active.Event is null) return [];
        var registrations = await store.RegistrationsAsync(active.Event.Id, ct);
        var claimed = false;
        foreach (var r in registrations.Where(r => r.OwnerId is null && actor.VerifiedEmail is not null && r.NormalizedEmail == Normalize(actor.VerifiedEmail)))
        {
            r.OwnerId = actor.Id;
            r.GuestTokenHash = "";
            r.Version = Guid.NewGuid();
            Audit(r.EventId, r.Id, actor, "registration.claimed");
            claimed = true;
        }
        if (claimed)
        {
            await CurrentAsync(active.Event.Id, true, ct);
            await store.SaveAsync(ct);
        }
        var results = new List<RegistrationView>();
        foreach (var r in registrations.Where(r => r.OwnerId == actor.Id)) results.Add(await ViewAsync(r, active.Event, actor, ct));
        return results;
    }
    public async Task<RegistrationView> GuestAsync(Guid id, string token, CancellationToken ct)
    {
        var r = await store.RegistrationAsync(id, ct);
        Require(r is not null && r.OwnerId is null && r.GuestTokenExpiresAt >= Now && token.Length == 64 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(r.GuestTokenHash), Encoding.UTF8.GetBytes(Hash(token))), 404, "Link ungültig oder abgelaufen. Bitte mit verifizierter E-Mail anmelden.");
        var settings = await CurrentAsync(r!.EventId, false, ct);
        return await ViewAsync(r, settings, new(null, null, new HashSet<string>()), ct);
    }
    public async Task<List<RegistrationView>> DeskAsync(Actor actor, CancellationToken ct)
    {
        Require(actor.Has(Permissions.Read), 403, "Keine Berechtigung.");
        var active = await ActiveAsync(ct);
        if (active.Event is null) return [];
        var result = new List<RegistrationView>();
        foreach (var r in (await store.RegistrationsAsync(active.Event.Id, ct)).OrderBy(r => r.CreatedAt))
            result.Add(await ViewAsync(r, active.Event, actor, ct));
        return result;
    }
    public async Task<RegistrationView> UpdateAsync(Guid id, UpdateSignup request, Actor actor, CancellationToken ct)
    {
        Validate(request.Form);
        var r = await LoadAsync(id, request.Version, ct);
        var settings = await CurrentAsync(r.EventId, true, ct);
        Require(actor.Has(Permissions.Manage) || CanEdit(r, settings, actor), 403, "Änderungen sind nur durch das Personal möglich.");
        Require(!r.Cancelled, 409, "Diese Anmeldung ist storniert.");
        Require(Normalize(request.Form.Email) == r.NormalizedEmail, 400, "Die Anmelde-E-Mail kann nicht geändert werden.");
        var old = Answers(r);
        var form = Clean(request.Form);
        if (!actor.Has(Permissions.Manage) && Now > settings.FoodDeadline)
            Require(old.Meal == form.Meal && old.MealQuantity == form.MealQuantity && old.IceCream == form.IceCream && old.Scoops == form.Scoops, 409, "Die Frist für Essenswünsche ist abgelaufen.");
        Require((!r.Paid && !r.CheckedIn) || (old.Attendance == form.Attendance && old.Sockets == form.Sockets), 409, "Vor einer Tarifänderung bitte Zahlung und Check-in zurücknehmen.");
        r.AnswersJson = JsonSerializer.Serialize(form);
        r.Version = Guid.NewGuid();
        Audit(r.EventId, id, actor, "registration.updated");
        await store.SaveAsync(ct);
        return await ViewAsync(r, settings, actor, ct);
    }
    public async Task<RegistrationView> ActionAsync(Guid id, DeskAction request, Actor actor, CancellationToken ct)
    {
        var r = await LoadAsync(id, request.Version, ct);
        var settings = await CurrentAsync(r.EventId, true, ct);
        var isCancel = request.Action == "cancel";
        Require(!r.Cancelled, 409, "Diese Anmeldung ist storniert.");
        if (isCancel)
        {
            Require(actor.Has(Permissions.Manage) || CanEdit(r, settings, actor), 403, "Stornierung ist nur durch das Personal möglich.");
            Require(!r.CheckedIn, 409, "Bitte zuerst den Check-in zurücknehmen.");
            r.Cancelled = true;
        }
        else if (request.Action is "staff" or "unstaff")
        {
            Require(actor.Has(Permissions.Admission), 403, "Keine Berechtigung für den Personalstatus.");
            Require(!r.Paid && !r.CheckedIn, 409, "Bitte zuerst Zahlung und Check-in zurücknehmen.");
            r.StaffApproved = request.Action == "staff";
        }
        else
        {
            Require(actor.Has(Permissions.Frontdesk), 403, "Keine Berechtigung für den Einlass.");
            var view = await ViewAsync(r, settings, actor, ct);
            switch (request.Action)
            {
                case "pay":
                    Require(view.PriceCents is > 0 && !r.Paid, 409, "Zahlung nicht möglich. Bitte Mitgliedsprüfung und Zahlungsstatus prüfen.");
                    r.Paid = true; r.SettledPriceCents = view.PriceCents; r.SettledAdmission = view.Admission; break;
                case "unpay":
                    Require(!r.CheckedIn && r.Paid, 409, "Bitte zuerst den Check-in zurücknehmen.");
                    r.Paid = false; r.SettledPriceCents = null; r.SettledAdmission = null; break;
                case "checkin":
                    Require(!r.CheckedIn && (r.Paid || view.PriceCents == 0), 409, "Bitte zuerst den Eintritt bezahlen bzw. die Berechtigung klären.");
                    r.CheckedIn = true; r.SettledPriceCents = view.PriceCents; r.SettledAdmission = view.Admission; break;
                case "uncheckin":
                    Require(r.CheckedIn, 409, "Noch nicht eingecheckt.");
                    r.CheckedIn = false;
                    if (!r.Paid) { r.SettledPriceCents = null; r.SettledAdmission = null; }
                    break;
                default: throw new GamenightException(400, "Unbekannte Aktion.");
            }
        }
        r.Version = Guid.NewGuid();
        Audit(r.EventId, id, actor, "registration." + request.Action);
        await store.SaveAsync(ct);
        return await ViewAsync(r, settings, actor, ct);
    }
    private async Task<Registration> LoadAsync(Guid id, Guid version, CancellationToken ct)
    {
        var r = await store.RegistrationAsync(id, ct);
        Require(r is not null, 404, "Anmeldung nicht gefunden.");
        Require(r!.Version == version, 409, "Die Anmeldung wurde zwischenzeitlich geändert. Bitte neu laden.");
        return r;
    }
    private bool CanEdit(Registration r, EventSettings e, Actor actor) => actor.Id is not null && r.OwnerId == actor.Id && !r.CheckedIn && !r.Cancelled && Now <= e.EditDeadline;
    private async Task<RegistrationView> ViewAsync(Registration r, EventSettings e, Actor actor, CancellationToken ct)
    {
        var form = Answers(r);
        var early = r.CreatedAt <= e.EarlyBirdDeadline;
        int? price;
        var member = false;
        string admission;
        if (r.SettledPriceCents is { } settled) { price = settled; admission = r.SettledAdmission ?? "Bezahlt"; member = admission.StartsWith("Mitglied"); }
        else if (r.StaffApproved) { price = 0; admission = "Personal – von Administration freigegeben"; }
        else if (form.Attendance == "Karaoke") { price = 0; admission = "Nur Karaoke – kostenlos"; }
        else
        {
            MembershipEligibility? eligibility = null;
            if (r.OwnerId is not null && e.MembershipPaymentPeriodId is { } period)
            {
                try { eligibility = await membership.GetAsync(r.OwnerId, period, ct); }
                catch (HttpRequestException) { eligibility = new(false, "unavailable"); }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested) { eligibility = new(false, "unavailable"); }
            }
            member = eligibility?.Eligible == true;
            if (eligibility?.Reason == "unavailable") { price = null; admission = "Mitgliedsprüfung derzeit nicht verfügbar"; }
            else if (member) { price = early ? e.MemberEarlyPriceCents : e.MemberRegularPriceCents; admission = "Mitglied – Beitrag in Management bezahlt"; }
            else { price = form.Sockets > 0 ? (early ? e.EarlySetupPriceCents : e.RegularSetupPriceCents) : (early ? e.EarlyPriceCents : e.RegularPriceCents); admission = early ? "Frühbucher" : "Regulär"; }
        }
        return new(r.Id, r.EventId, form, r.Version, r.Cancelled, r.StaffApproved, r.CheckedIn, r.Paid, price, admission, r.CreatedAt, CanEdit(r, e, actor), member);
    }
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static DateTime Utc(DateTime date) => date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : DateTime.SpecifyKind(date, DateTimeKind.Utc);
    private static SignupForm Clean(SignupForm form)
    {
        form.Email = form.Email.Trim(); form.FirstName = form.FirstName.Trim(); form.LastName = form.LastName.Trim();
        if (form.Attendance == "Karaoke") { form.Sockets = null; form.Meal = null; form.MealQuantity = null; form.IceCream = null; form.Scoops = null; form.PenAndPaper = null; form.GameNightRules = false; }
        if (form.Meal == "Nichts") form.MealQuantity = null;
        if (string.IsNullOrEmpty(form.IceCream) || form.IceCream == "Nein") form.Scoops = null;
        return form;
    }
}
