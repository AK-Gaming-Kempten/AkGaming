using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;

namespace MemberManagement.Contracts.Services;

public interface IMemberAuditLogService {
    Task<Result<MemberAuditLogsResponseDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null);
}
