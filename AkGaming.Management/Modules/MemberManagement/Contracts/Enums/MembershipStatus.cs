using System.Text.Json.Serialization;

namespace AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

/// <summary>
/// Mirror of <see cref="MembershipStatus"/> used for contracts
/// </summary>
/// 
public enum MembershipStatus {
    None,
    Expelled,
    Suspended,
    Withdrawn,
    Applicant,
    InTrial,
    Member,
    HonoraryMember,
    ApplicationRejected,
    SupportingMember
}