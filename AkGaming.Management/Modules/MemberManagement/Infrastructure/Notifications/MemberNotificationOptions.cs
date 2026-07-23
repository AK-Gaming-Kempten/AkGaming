namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Notifications;

public sealed class MemberNotificationOptions
{
    public const string SectionName = "Notifications";
    public string Endpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = "akgaming-management-api";
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "gamelybot_notifications";
    public bool UseAuthentication { get; set; } = true;
    public string? ManagementFrontendBaseUrl { get; set; }
    public string? ManagementBaseUrl { get; set; }
}
