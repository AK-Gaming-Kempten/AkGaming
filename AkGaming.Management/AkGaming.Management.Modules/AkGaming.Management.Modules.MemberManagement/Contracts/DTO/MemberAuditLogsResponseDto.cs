namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

public class MemberAuditLogsResponseDto {
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyCollection<MemberAuditLogItemDto> Items { get; set; } = Array.Empty<MemberAuditLogItemDto>();
}
