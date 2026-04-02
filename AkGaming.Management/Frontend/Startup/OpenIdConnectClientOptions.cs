namespace AkGaming.Management.Frontend.Startup;

public sealed class OpenIdConnectClientOptions
{
    public const string SectionName = "OpenIdConnect";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";
    public bool RequireHttpsMetadata { get; set; } = true;
    public List<string> Scopes { get; set; } = [];
}
