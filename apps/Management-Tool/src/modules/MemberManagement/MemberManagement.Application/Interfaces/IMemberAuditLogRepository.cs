using AKG.Common.Generics;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

public interface IMemberAuditLogRepository {
    Task<Result<MemberAuditLogQueryResult>> GetPagedAsync(int page, int pageSize, string? search = null);
}

public class MemberAuditLogQueryResult {
    public int TotalCount { get; set; }
    public IReadOnlyCollection<MemberAuditLog> Items { get; set; } = Array.Empty<MemberAuditLog>();
}
