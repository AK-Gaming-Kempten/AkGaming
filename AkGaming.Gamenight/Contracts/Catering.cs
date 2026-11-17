using System.ComponentModel.DataAnnotations;

namespace AkGaming.Gamenight.Contracts;

public static class CateringIcons
{
    public static readonly string[] All = ["meal", "pizza", "doner", "durum"];
}
public sealed class CateringMenu
{
    public Guid EventId { get; set; }
    public Guid Version { get; set; }
    public bool OrdersOpen { get; set; }
    [Range(0, 100000)] public int DefaultDiscountCents { get; set; }
    public List<CateringCategory> Categories { get; set; } = [];
    public List<CateringItem> Items { get; set; } = [];
}
public sealed class CateringCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public int Position { get; set; }
}
public sealed class CateringItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [Range(0, 100000)] public int PriceCents { get; set; }
    public bool Available { get; set; } = true;
    public bool DiscountEligible { get; set; } = true;
    [Range(0, 100000)] public int? DiscountOverrideCents { get; set; }
    public string Icon { get; set; } = "meal";
    public int Position { get; set; }
}
public sealed class CateringLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Icon { get; set; } = "meal";
    public int UnitPriceCents { get; set; }
    public int Quantity { get; set; }
    public int PossibleDiscountCents { get; set; }
    public int DiscountCents { get; set; }
    public bool DiscountApplied { get; set; }
    public bool Paid { get; set; }
    public int CollectedQuantity { get; set; }
    public DateTime OrderedAt { get; set; }
    public int TotalCents => UnitPriceCents * Quantity - DiscountCents;
}
public sealed record CateringView(Guid RegistrationId, Guid Version, string Person, string Email,
    bool Cancelled, bool EarlyBird, bool? EligibilityOverride, List<CateringLine> Lines)
{
    public bool DiscountEligible => EligibilityOverride ?? EarlyBird;
    public int OutstandingCents => Lines.Where(l => !l.Paid).Sum(l => l.TotalCents);
}
public sealed record CateringQuantity(Guid ItemId, int Quantity);
public sealed record PlaceCateringOrder(Guid EventId, Guid MenuVersion, Guid Version,
    List<CateringQuantity> Items, bool AcknowledgeLocked = false);
public sealed record CateringAction(Guid EventId, Guid Version, string Action,
    Guid? LineId = null, int? Quantity = null, bool? EligibilityOverride = null, string? Reason = null);
public sealed record SettleVisitor(Guid EventId, Guid RegistrationVersion, Guid CateringVersion, int? ExpectedAdmissionCents = null);
