using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMemberAuditLogRepository {
    Task<Result<MemberAuditLogQueryResult>> GetPagedAsync(int page, int pageSize, string? search = null);
}

public class MemberAuditLogQueryResult {
    public int TotalCount { get; set; }
    public IReadOnlyCollection<MemberAuditLog> Items { get; set; } = Array.Empty<MemberAuditLog>();
}
