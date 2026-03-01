using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Services;

public interface IMemberAuditLogService {
    Task<Result<MemberAuditLogsResponseDto>> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null);
}
