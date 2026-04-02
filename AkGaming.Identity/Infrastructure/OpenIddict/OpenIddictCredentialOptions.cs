namespace AkGaming.Identity.Infrastructure.OpenIddict;

public sealed class OpenIddictCredentialOptions
{
    public const string SectionName = "OpenIddict:Credentials";

    public OpenIddictCertificateOptions Signing { get; set; } = new();
    public OpenIddictCertificateOptions Encryption { get; set; } = new();
}

public sealed class OpenIddictCertificateOptions
{
    public string? Path { get; set; }
    public string? Password { get; set; }
}
