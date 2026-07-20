namespace AkGaming.GamelyBot.Infrastructure;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";
    public string Token { get; set; } = string.Empty;
    public string GuildId { get; set; } = string.Empty;
    public string AdministrationChannelId { get; set; } = string.Empty;
    public string TreasurerRoleId { get; set; } = string.Empty;
}

public sealed class IdentityClientOptions
{
    public const string SectionName = "IdentityClient";
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "identity_discord_links";
    public bool UseAuthentication { get; set; } = true;
    public string? DebugDiscordUserId { get; set; } = "debug-discord-user";
}
