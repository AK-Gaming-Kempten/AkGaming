using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMemberAuditLogRepository : IMemberAuditLogRepository {
    private readonly MemberManagementDbContext _dbContext;

    public EfMemberAuditLogRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<Result<MemberAuditLogQueryResult>> GetPagedAsync(int page, int pageSize, string? search = null) {
        try {
            var query = _dbContext.MemberAuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search)) {
                var trimmedSearch = search.Trim();
                var loweredSearch = trimmedSearch.ToLower();
                var isGuidSearch = Guid.TryParse(trimmedSearch, out var searchedGuid);

                query = query.Where(x =>
                    x.ActionType.ToLower().Contains(loweredSearch) ||
                    x.EntityType.ToLower().Contains(loweredSearch) ||
                    (x.OldValuesJson != null && x.OldValuesJson.ToLower().Contains(loweredSearch)) ||
                    (x.NewValuesJson != null && x.NewValuesJson.ToLower().Contains(loweredSearch)) ||
                    (isGuidSearch && (x.EntityId == searchedGuid || x.PerformedByUserId == searchedGuid))
                );
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.OccurredAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Result<MemberAuditLogQueryResult>.Success(new MemberAuditLogQueryResult {
                TotalCount = totalCount,
                Items = items
            });
        }
        catch (Exception ex) {
            return Result<MemberAuditLogQueryResult>.Failure($"Database error: {ex.Message}");
        }
    }
}
