namespace AkGaming.Management.Modules.Disbursements.Contracts.DTO;

public sealed class DiscordGuildCatalogDto
{
    public List<DiscordChannelDto> Channels { get; set; } = [];
    public List<DiscordRoleDto> Roles { get; set; } = [];
}

public sealed class DiscordChannelDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Position { get; set; }
}

public sealed class DiscordRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
}
