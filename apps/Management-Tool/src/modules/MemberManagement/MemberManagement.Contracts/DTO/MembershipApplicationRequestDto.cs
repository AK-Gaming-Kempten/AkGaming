namespace MemberManagement.Contracts.DTO;

public class MembershipApplicationRequestDto {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IssuingUserId { get; set; }
    public MemberCreationDto MemberCreationInfo { get; set; } = new();
    public string ApplicationText { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
}