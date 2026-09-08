namespace AkGaming.Gamenight.Domain;
public sealed class GamenightEvent
{
    public Guid Id { get; set; }
    public string SettingsJson { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
}
public sealed class EventSelection
{
    public int Id { get; set; } = 1;
    public Guid? EventId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public sealed class Registration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string NormalizedEmail { get; set; } = "";
    public string? OwnerId { get; set; }
    public string AnswersJson { get; set; } = "";
    public string GuestTokenHash { get; set; } = "";
    public DateTime GuestTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Cancelled { get; set; }
    public bool StaffApproved { get; set; }
    public bool CheckedIn { get; set; }
    public bool Paid { get; set; }
    public int? SettledPriceCents { get; set; }
    public string? SettledAdmission { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid? RegistrationId { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime At { get; set; }
}
public sealed class EmailOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Recipient { get; set; } = "";
    public string ProtectedBody { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public int Attempts { get; set; }
}

