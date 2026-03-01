using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;

namespace AkGaming.Management.Modules.MemberManagement.Application.Interfaces;

public interface IMemberAuditLogWriter {
    Result Add(MemberAuditLog auditLog);
}
