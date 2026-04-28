namespace AkGaming.Tournaments.Contracts.DTOs;

public sealed record TournamentInfoSectionDto(
    Guid Id,
    string Header,
    string ContentMarkdown,
    int SortOrder);
