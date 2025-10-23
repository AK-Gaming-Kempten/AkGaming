namespace Membermanagement.Contracts.Enums;

/// <summary>
/// Mirror of <see cref="MemberManagement.Domain.Enums.MembershipStatus"/> used for contracts
/// </summary>
public enum MembershipStatus {
    None,
    Expelled,
    Suspended,
    Withdrawn,
    Applicant,
    InTrial,
    Member,
    HonoraryMember
}