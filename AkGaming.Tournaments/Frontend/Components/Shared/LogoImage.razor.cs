using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class LogoImage : ComponentBase
{
    [Parameter] public Guid? LogoAssetId { get; set; }
    [Parameter] public string Type { get; set; } = "generic";
    [Parameter] public string Alt { get; set; } = "Logo";
    [Parameter] public LogoImageSize Size { get; set; } = LogoImageSize.Medium;

    private string GetLogoUrl(Guid logoAssetId)
        => $"/media-assets/{logoAssetId}/file";

    private string GetFallbackIcon()
        => Type.ToLowerInvariant() switch
        {
            "game" => "bi-controller",
            "team" => "bi-people-fill",
            "player" => "bi-person-badge-fill",
            "tournament" => "bi-trophy-fill",
            _ => "bi-image-fill"
        };
}

public enum LogoImageSize
{
    Small,
    Medium,
    Large
}
