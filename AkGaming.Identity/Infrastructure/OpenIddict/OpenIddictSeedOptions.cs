namespace AkGaming.Identity.Infrastructure.OpenIddict;

public sealed class OpenIddictSeedOptions
{
    public const string SectionName = "OpenIddict";

    public string? Issuer { get; set; }
    public List<OpenIddictApplicationSeed> Applications { get; set; } = [];
    public List<OpenIddictScopeSeed> Scopes { get; set; } = [];
}

public sealed class OpenIddictApplicationSeed
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ConsentType { get; set; } = "implicit";
    public string ClientType { get; set; } = "public";
    public bool RequirePkce { get; set; } = true;
    public bool AllowAuthorizationCodeFlow { get; set; } = true;
    public bool AllowRefreshTokenFlow { get; set; } = true;
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
}

public sealed class OpenIddictScopeSeed
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Resources { get; set; } = [];
}
