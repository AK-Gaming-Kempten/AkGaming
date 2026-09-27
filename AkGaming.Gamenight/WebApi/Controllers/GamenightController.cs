using System.Security.Claims;
using System.Text;
using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AkGaming.Gamenight.WebApi.Controllers;

[ApiController, Route("api")]
public sealed class GamenightController(IGamenightService service, IMembershipClient membership) : ControllerBase
{
    private Actor Actor => new(User.Identity?.IsAuthenticated == true ? User.FindFirstValue("sub") : null,
        User.HasClaim("email_verified", "true") ? User.FindFirstValue("email") : null,
        User.FindAll("permission").Select(c => c.Value).ToHashSet());
    [HttpGet("events/active"), AllowAnonymous]
    public async Task<ActionResult<ActiveEvent>> Active(CancellationToken ct)
    {
        var result = await service.ActiveAsync(ct);
        return Ok(result);
    }
    [HttpGet("events"), Authorize(Policy = Permissions.Events)]
    public async Task<ActionResult<List<EventSettings>>> Events(CancellationToken ct)
    {
        var result = await service.EventsAsync(Actor, ct);
        return Ok(result);
    }
    [HttpPost("events"), Authorize(Policy = Permissions.Events)]
    public async Task<ActionResult<EventSettings>> SaveEvent(EventSettings request, CancellationToken ct)
    {
        var result = await service.SaveEventAsync(request, Actor, ct);
        return Ok(result);
    }
    [HttpPost("events/active"), Authorize(Policy = Permissions.Events)]
    public async Task<IActionResult> Activate(ActivateEvent request, CancellationToken ct)
    {
        await service.ActivateAsync(request, Actor, ct);
        return NoContent();
    }
    [HttpGet("membership-periods"), Authorize(Policy = Permissions.Events)]
    public async Task<ActionResult<List<PaymentPeriodOption>>> Periods(CancellationToken ct)
    {
        var result = await membership.PeriodsAsync(ct);
        return Ok(result);
    }
    [HttpPost("registrations"), AllowAnonymous, EnableRateLimiting("guest")]
    public async Task<ActionResult<Receipt>> Submit(SubmitSignup request, CancellationToken ct)
    {
        // Authenticated requests must target this API, even on the guest-capable route.
        if (User.Identity?.IsAuthenticated == true && !User.FindAll("scope").SelectMany(c => c.Value.Split(' ')).Contains("gamenight_api"))
            return Forbid();
        await service.SubmitAsync(request, Actor, ct);
        return Accepted(new Receipt("Wenn diese E-Mail neu angemeldet wurde, erhältst du eine Bestätigung mit einem privaten Ansichtslink. Bestehende Anmeldungen kannst du über dein Konto verwalten."));
    }
    [HttpPost("registrations/mine"), Authorize]
    public async Task<ActionResult<List<RegistrationView>>> Mine(CancellationToken ct)
    {
        var result = await service.MineAsync(Actor, ct);
        return Ok(result);
    }
    [HttpPost("registrations/{id:guid}/guest"), AllowAnonymous, EnableRateLimiting("guest")]
    public async Task<ActionResult<RegistrationView>> Guest(Guid id, [FromBody] string token, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await service.GuestAsync(id, token, ct);
        return Ok(result);
    }
    [HttpGet("registrations"), Authorize(Policy = Permissions.Read)]
    public async Task<ActionResult<List<RegistrationView>>> Desk(CancellationToken ct)
    {
        var result = await service.DeskAsync(Actor, ct);
        return Ok(result);
    }
    [HttpPut("registrations/{id:guid}"), Authorize]
    public async Task<ActionResult<RegistrationView>> Update(Guid id, UpdateSignup request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, Actor, ct);
        return Ok(result);
    }
    [HttpPost("registrations/{id:guid}/actions"), Authorize]
    public async Task<ActionResult<RegistrationView>> Action(Guid id, DeskAction request, CancellationToken ct)
    {
        var result = await service.ActionAsync(id, request, Actor, ct);
        return Ok(result);
    }
    [HttpGet("registrations/export"), Authorize(Policy = Permissions.Export), Authorize(Policy = Permissions.Read)]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var rows = await service.DeskAsync(Actor, ct);
        var csv = new StringBuilder("Vorname;Nachname;E-Mail;Teilnahme;Personal;Storniert;Check-in;Bezahlt;Preis (Cent);Tarif;Steckdosen;Möchte bestellen;Döner;Pizza;Eiskugeln;Eiswunsch;Pen & Paper;Orga-Angabe;Anmerkungen;Quellen;Sonstiges;Haftung;Regeln;Fotos;Hardware;Game-Night-Regeln\r\n");
        foreach (var r in rows)
            csv.AppendLine(string.Join(";", new object?[] { r.Form.FirstName, r.Form.LastName, r.Form.Email, r.Form.Attendance, r.StaffApproved, r.Cancelled, r.CheckedIn, r.Paid, r.PriceCents, r.Admission, r.Form.Sockets, r.Form.WantsToOrder, r.Form.DonerQuantity ?? (r.Form.Meal is "Döner" or "Döner, Pizza" ? r.Form.MealQuantity : 0), r.Form.PizzaQuantity ?? (r.Form.Meal is "Pizza" or "Döner, Pizza" ? r.Form.MealQuantity : 0), r.Form.IceCreamQuantity ?? r.Form.Scoops, r.Form.IceCream, r.Form.PenAndPaper, r.Form.OrganizerDeclaration, r.Form.Notes, string.Join(", ", r.Form.DiscoverySources), r.Form.DiscoveryOther, r.Form.Liability, r.Form.GeneralRules, r.Form.PhotoConsent, r.Form.HardwareResponsibility, r.Form.GameNightRules }.Select(Cell)));
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "gamenight-anmeldungen.csv");
    }
    private static string Cell(object? value)
    {
        var text = value?.ToString() ?? "";
        if (text.TrimStart().StartsWith('=') || text.TrimStart().StartsWith('+') || text.TrimStart().StartsWith('-') || text.TrimStart().StartsWith('@'))
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
