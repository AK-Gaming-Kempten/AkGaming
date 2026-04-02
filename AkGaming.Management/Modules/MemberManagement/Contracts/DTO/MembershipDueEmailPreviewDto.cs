namespace AkGaming.Management.Modules.MemberManagement.Contracts.DTO;

/// <summary>
/// Preview payload for a membership due reminder email.
/// </summary>
public class MembershipDueEmailPreviewDto {
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientDisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}
