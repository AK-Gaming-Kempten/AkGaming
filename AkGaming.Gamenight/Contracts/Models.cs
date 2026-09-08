using System.ComponentModel.DataAnnotations;

namespace AkGaming.Gamenight.Contracts;

public static class Permissions
{
    public const string Events = "gamenight.events.manage";
    public const string Read = "gamenight.registrations.read";
    public const string Manage = "gamenight.registrations.manage";
    public const string Export = "gamenight.registrations.export";
    public const string Admission = "gamenight.admission.manage";
    public const string Frontdesk = "gamenight.frontdesk.manage";
    public static readonly string[] All = [Events, Read, Manage, Export, Admission, Frontdesk];
}

public sealed class EventSettings : IValidatableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public DateTime StartsAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime EndsAt { get; set; } = DateTime.UtcNow.AddDays(31);
    public DateTime SignupDeadline { get; set; } = DateTime.UtcNow.AddDays(29);
    public DateTime EditDeadline { get; set; } = DateTime.UtcNow.AddDays(29);
    public DateTime EarlyBirdDeadline { get; set; } = DateTime.UtcNow.AddDays(28);
    public DateTime FoodDeadline { get; set; } = DateTime.UtcNow.AddDays(28);
    [Required, StringLength(200)] public string Location { get; set; } = "Hochschule Kempten, S-Gebäude";
    [StringLength(8000)] public string Information { get; set; } = "Einlass von 17:00 bis 22:00 Uhr. Teilnahme ab 18 Jahren; bitte einen gültigen Lichtbildausweis mitbringen. Zahlung vor Ort in bar. Für ein eigenes Setup bitte ein mindestens 5 m langes LAN-Kabel mitbringen.";
    [Range(0, 100000)] public int EarlyPriceCents { get; set; } = 500;
    [Range(0, 100000)] public int EarlySetupPriceCents { get; set; } = 700;
    [Range(0, 100000)] public int RegularPriceCents { get; set; } = 700;
    [Range(0, 100000)] public int RegularSetupPriceCents { get; set; } = 900;
    [Range(0, 100000)] public int MemberEarlyPriceCents { get; set; } = 500;
    [Range(0, 100000)] public int MemberRegularPriceCents { get; set; } = 700;
    [Range(1, int.MaxValue)] public int? MembershipPaymentPeriodId { get; set; }
    public Guid Version { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndsAt <= StartsAt) yield return new("Das Ende muss nach dem Beginn liegen.");
        if (SignupDeadline > EndsAt || EditDeadline > EndsAt || FoodDeadline > EndsAt || EarlyBirdDeadline > EndsAt)
            yield return new("Fristen müssen spätestens am Veranstaltungsende liegen.");
    }
}

public sealed class SignupForm : IValidatableObject
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, RegularExpression("GameNight|Karaoke")] public string Attendance { get; set; } = "GameNight";
    public int? Sockets { get; set; }
    public string? Meal { get; set; }
    [Range(1, 5)] public int? MealQuantity { get; set; }
    public string? IceCream { get; set; }
    [Range(1, 10)] public int? Scoops { get; set; }
    public bool? PenAndPaper { get; set; }
    public bool GameNightRules { get; set; }
    [StringLength(4000)] public string? Notes { get; set; }
    public bool Liability { get; set; }
    public bool GeneralRules { get; set; }
    public bool PhotoConsent { get; set; }
    public bool HardwareResponsibility { get; set; }
    public List<string> DiscoverySources { get; set; } = [];
    [StringLength(500)] public string? DiscoveryOther { get; set; }
    [Required] public bool? OrganizerDeclaration { get; set; }
    public static readonly string[] Sources = ["Mundpropaganda", "Hochschule (Flyer & Plakate)", "Über einen Discord Server", "Social Media", "Rundmail", "Flyer & Plakate außerhalb der Hochschule (z.B. Heldenschmiede)", "AK Gaming Website", "Google & andere Websiten", "Emil.", "Steve", "Sonstiges"];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Liability || !GeneralRules || !PhotoConsent || !HardwareResponsibility)
            yield return new("Bitte alle erforderlichen Einverständniserklärungen bestätigen.");
        if (Attendance == "GameNight")
        {
            if (Sockets is not (0 or 1 or 2 or 4)) yield return new("Bitte den Steckdosenbedarf auswählen.");
            if (Meal is not ("Döner" or "Pizza" or "Nichts")) yield return new("Bitte den Essenswunsch auswählen.");
            if (PenAndPaper is null || !GameNightRules) yield return new("Bitte Pen & Paper beantworten und die Game-Night-Regeln bestätigen.");
            if (IceCream is not (null or "" or "Ja" or "Nein" or "Nur Laktosefreies Eis")) yield return new("Ungültige Eisauswahl.");
        }
        if (DiscoverySources.Any(s => !Sources.Contains(s)) || DiscoverySources.Count > Sources.Length)
            yield return new("Ungültige Angabe zur Veranstaltungswerbung.");
    }
}
public sealed record SubmitSignup(Guid EventId, SignupForm Form);
public sealed record UpdateSignup(Guid Version, SignupForm Form);
public sealed record DeskAction(Guid Version, string Action, string? Reason = null);
public sealed record ActiveEvent(EventSettings? Event, Guid SelectionVersion);
public sealed record ActivateEvent(Guid EventId, Guid SelectionVersion);
public sealed record Receipt(string Message);
public sealed record RegistrationView(Guid Id, Guid EventId, SignupForm Form, Guid Version,
    bool Cancelled, bool StaffApproved, bool CheckedIn, bool Paid, int? PriceCents, string Admission,
    DateTime CreatedAt, bool CanEdit, bool MemberEligible);
public sealed record MembershipEligibility(bool Eligible, string Reason);
public sealed record PaymentPeriodOption(int Id, string Name);

