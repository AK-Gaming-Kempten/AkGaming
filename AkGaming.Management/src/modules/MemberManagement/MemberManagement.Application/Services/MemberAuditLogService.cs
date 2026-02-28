using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberAuditLogService : IMemberAuditLogService {
    private readonly IMemberAuditLogRepository _auditLogRepository;

    public MemberAuditLogService(IMemberAuditLogRepository auditLogRepository) {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<MemberAuditLogsResponseDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, string? search = null) {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var result = await _auditLogRepository.GetPagedAsync(page, pageSize, search);
        if (!result.IsSuccess || result.Value is null) {
            return Result<MemberAuditLogsResponseDto>.Failure(result.Error ?? "Failed to retrieve member audit logs.");
        }

        var payload = new MemberAuditLogsResponseDto {
            Page = page,
            PageSize = pageSize,
            TotalCount = result.Value.TotalCount,
            Items = result.Value.Items
                .Select(x => new MemberAuditLogItemDto {
                    Id = x.Id,
                    OccurredAtUtc = x.OccurredAtUtc,
                    ActionType = x.ActionType,
                    PerformedByUserId = x.PerformedByUserId,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    OldValuesJson = x.OldValuesJson,
                    NewValuesJson = x.NewValuesJson
                })
                .ToArray()
        };

        return Result<MemberAuditLogsResponseDto>.Success(payload);
    }
}
