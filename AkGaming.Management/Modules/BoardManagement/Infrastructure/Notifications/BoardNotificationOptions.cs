namespace AkGaming.Management.Modules.BoardManagement.Infrastructure.Notifications;

public sealed class BoardNotificationOptions
{
    public const string SectionName = "Notifications";
    public string Endpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = "akgaming-management-api";
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "gamelybot_notifications";
    public bool UseAuthentication { get; set; } = true;
    public string? ManagementBaseUrl { get; set; }
}
