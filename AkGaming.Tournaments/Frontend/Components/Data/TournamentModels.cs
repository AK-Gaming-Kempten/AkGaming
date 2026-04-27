namespace AkGaming.Tournaments.Frontend.Components.Data;

public sealed record TournamentSummary(
    string Slug,
    string Title,
    string Season,
    string Game,
    string Format,
    string Venue,
    string RegistrationWindow,
    string Status,
    string StatusTone,
    string Description,
    int RegisteredTeams,
    int TotalSlots);

public sealed record PageMetric(
    string Label,
    string Value,
    string Caption);

public sealed record StatusListItem(
    string Eyebrow,
    string Title,
    string Description,
    string Meta,
    string Tone = "neutral");

public sealed record TournamentInfoField(
    string Header,
    string Text);

public sealed record TournamentTimelineEntry(
    string Label,
    string Date,
    string Description,
    string Tone = "neutral");

public sealed record MatchCardModel(
    string Stage,
    string Window,
    string Arena,
    string LeftName,
    string RightName,
    string LeftMeta,
    string RightMeta,
    string Status,
    string Tone,
    string Note);

public sealed record TeamMember(
    string Handle,
    string Role,
    string Availability);

public sealed record TeamCardModel(
    string Name,
    string Region,
    string Standing,
    string Note,
    IReadOnlyList<TeamMember> Members);

public sealed record TournamentDetail(
    TournamentSummary Summary,
    string Tagline,
    string HeroDescription,
    string NextMilestone,
    IReadOnlyList<PageMetric> Metrics,
    IReadOnlyList<StatusListItem> Updates,
    IReadOnlyList<MatchCardModel> Matches,
    IReadOnlyList<TeamCardModel> Teams,
    IReadOnlyList<StatusListItem> PlayerTasks,
    IReadOnlyList<StatusListItem> AdminQueue,
    IReadOnlyList<StatusListItem> Timeline)
{
    public string LogoUrl => Summary.Game switch
    {
        "League of Legends" => "images/icons/AKG_Logos/Green.png",
        "VALORANT" => "images/icons/AKG_Logos/Red.png",
        "EA Sports FC" => "images/icons/AKG_Logos/Blue.png",
        _ => "images/icons/AKG_Logos/Default.png"
    };

    public IReadOnlyList<TournamentInfoField> InfoFields =>
    [
        new("Game", Summary.Game),
        new("Format", Summary.Format),
        new("Venue", Summary.Venue),
        new("Capacity", $"{Summary.RegisteredTeams} of {Summary.TotalSlots} teams registered")
    ];

    public IReadOnlyList<TournamentTimelineEntry> KeyDates =>
    [
        new("Registration", Summary.RegistrationWindow, "Teams can prepare rosters and submit their tournament registration.", Summary.StatusTone),
        new("Briefing", NextMilestone, "Captains receive final operational notes, rules reminders, and check-in expectations.", "warn"),
        new("Tournament start", Matches.FirstOrDefault()?.Window ?? "To be announced", "The first scheduled matches begin once registration and roster checks are complete.", "positive")
    ];
}
