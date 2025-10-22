namespace MemberManagement.Domain.Entities;

using MemberManagement.Domain.Enums;
using MemberManagement.Domain.ValueObjects;

public class Member {
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public Address Address { get; set; }
    public MembershipStatus Status { get; set; }
    public DateOnly MembershipStartDate { get; set; }
    public DateOnly? TrialEndDate { get; set; }
    public DateOnly? ExpulsionDate { get; set; }
    public DateOnly? SuspensionStartDate { get; set; }
    public DateOnly? SuspensionEndDate { get; set; }
    public DateOnly? WithdrawalDate { get; set; }
    public DateOnly? HonoraryMemberDate { get; set; }
}