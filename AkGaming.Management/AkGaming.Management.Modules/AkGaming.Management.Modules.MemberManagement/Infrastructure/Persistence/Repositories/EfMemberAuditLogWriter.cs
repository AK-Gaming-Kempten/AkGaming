using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

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
