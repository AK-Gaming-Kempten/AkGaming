namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class DevelopmentAdminSeedOptions
{
    public const string SectionName = "DevelopmentAdmin";

    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
