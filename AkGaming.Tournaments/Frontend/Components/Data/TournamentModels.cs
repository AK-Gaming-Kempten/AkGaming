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
    IReadOnlyList<StatusListItem> Timeline);
