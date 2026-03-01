namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

public class MemberAuditLogItemDto {
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Guid? PerformedByUserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
}
