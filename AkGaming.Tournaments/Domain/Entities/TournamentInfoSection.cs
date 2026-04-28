namespace AkGaming.Tournaments.Domain.Entities;

public sealed class TournamentInfoSection
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Tournament? Tournament { get; set; }
}
