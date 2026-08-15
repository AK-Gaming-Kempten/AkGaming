namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class AllocationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public Guid ShareToken { get; set; }
    public string DiscordChannelId { get; set; } = string.Empty;
    public string DiscordChannelName { get; set; } = string.Empty;
    public string DiscordRoleId { get; set; } = string.Empty;
    public string DiscordRoleName { get; set; } = string.Empty;
    public decimal AppliedAmount => Applications
        .Where(application => application.Status is not Enums.AllocationApplicationStatus.Rejected
            and not Enums.AllocationApplicationStatus.Cancelled)
        .Sum(application => application.Amount);
    public List<AllocationApplicationDto> Applications { get; set; } = [];
}

public sealed class SaveAllocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string DiscordChannelId { get; set; } = string.Empty;
    public string DiscordChannelName { get; set; } = string.Empty;
    public string DiscordRoleId { get; set; } = string.Empty;
    public string DiscordRoleName { get; set; } = string.Empty;
}
