using AkGaming.Identity.Application.Abstractions;

namespace AkGaming.Identity.Infrastructure.Security;

public sealed class AppUrlOptions : IAppUrlSettings
{
    public const string SectionName = "App";

    public string PublicBaseUrl { get; set; } = "https://localhost:5001";
}
