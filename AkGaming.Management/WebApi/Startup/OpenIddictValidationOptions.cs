namespace AkGaming.Management.WebApi.Startup;

public sealed class OpenIddictValidationOptions
{
    public const string SectionName = "OpenIddictValidation";

    public string Issuer { get; set; } = string.Empty;
}
