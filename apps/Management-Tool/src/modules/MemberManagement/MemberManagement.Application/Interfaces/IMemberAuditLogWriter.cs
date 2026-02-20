using AKG.Common.Generics;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

public interface IMemberAuditLogWriter {
    Result Add(MemberAuditLog auditLog);
}
