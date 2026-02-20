using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMemberAuditLogWriter : IMemberAuditLogWriter {
    private readonly MemberManagementDbContext _dbContext;

    public EfMemberAuditLogWriter(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public Result Add(MemberAuditLog auditLog) {
        try {
            _dbContext.Set<MemberAuditLog>().Add(auditLog);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to add audit log: {ex.Message}");
        }
    }
}
