namespace AkGaming.Management.Modules.MemberManagement.Domain.Entities;

public class MembershipPaymentPeriod {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal DefaultDueAmount { get; set; }
    public decimal ReducedDueAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<MembershipDue> Dues { get; set; } = new List<MembershipDue>();
}
