namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Public;

public partial class TournamentOverview
{
    private const string DefaultMediumGreen = "#1f7a52";
    private const string DefaultDarkGreen = "#0f3f2a";

    private static string GetBannerStyle(AkGaming.Tournaments.Contracts.DTOs.TournamentDto tournament)
    {
        var baseColor = string.IsNullOrWhiteSpace(tournament.PrimaryColor) ? DefaultDarkGreen : tournament.PrimaryColor!;
        var bannerUrl = tournament.BannerAssetId is Guid bannerAssetId ? $"/media-assets/{bannerAssetId}/file" : null;
        var imageLayer = bannerUrl is null ? "none" : $"url('{bannerUrl}')";
        var leftColor = bannerUrl is null ? DefaultMediumGreen : baseColor;
        return $"--tournament-banner-left:{leftColor};--tournament-banner-base:{baseColor};--tournament-banner-image:{imageLayer};";
    }
}
