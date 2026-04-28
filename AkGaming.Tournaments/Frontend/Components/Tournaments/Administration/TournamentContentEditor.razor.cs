using AkGaming.Tournaments.Contracts.DTOs;
using Microsoft.AspNetCore.Components;

namespace AkGaming.Tournaments.Frontend.Components.Tournaments.Administration;

public partial class TournamentContentEditor : ComponentBase
{
    [Parameter] public TournamentDto Tournament { get; set; } = default!;
    [Parameter] public bool IsBusy { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback<TournamentContentSaveRequest> SaveRequested { get; set; }

    private string? currentTournamentStateKey;
    private string? registrationOpenValue;
    private string? registrationClosedValue;
    private string? startValue;
    private string? endValue;
    private List<EditableTournamentInfoSection> sections = [];

    protected override void OnParametersSet()
    {
        var stateKey = BuildStateKey(Tournament);
        if (currentTournamentStateKey == stateKey)
            return;

        currentTournamentStateKey = stateKey;
        registrationOpenValue = ToInputValue(Tournament.RegistrationOpenUtc);
        registrationClosedValue = ToInputValue(Tournament.RegistrationClosedUtc);
        startValue = ToInputValue(Tournament.StartUtc);
        endValue = ToInputValue(Tournament.EndUtc);
        sections = Tournament.InfoSections
            .OrderBy(section => section.SortOrder)
            .Select(section => new EditableTournamentInfoSection(section.Id, section.Header, section.ContentMarkdown))
            .ToList();

        if (sections.Count == 0)
        {
            AddSection();
        }
    }

    private void AddSection()
    {
        sections.Add(new EditableTournamentInfoSection(Guid.NewGuid(), string.Empty, string.Empty));
    }

    private void RemoveSection(Guid sectionId)
    {
        sections.RemoveAll(section => section.Id == sectionId);
        if (sections.Count == 0)
        {
            AddSection();
        }
    }

    private void HandleRegistrationOpenChanged(ChangeEventArgs args) => registrationOpenValue = args.Value?.ToString();
    private void HandleRegistrationClosedChanged(ChangeEventArgs args) => registrationClosedValue = args.Value?.ToString();
    private void HandleStartChanged(ChangeEventArgs args) => startValue = args.Value?.ToString();
    private void HandleEndChanged(ChangeEventArgs args) => endValue = args.Value?.ToString();

    private Task SaveAsync()
    {
        var request = new TournamentContentSaveRequest(
            ParseInputValue(registrationOpenValue),
            ParseInputValue(registrationClosedValue),
            ParseInputValue(startValue),
            ParseInputValue(endValue),
            sections.Select((section, index) => new TournamentInfoSectionDto(section.Id, section.Header, section.ContentMarkdown, index)).ToList());
        return SaveRequested.InvokeAsync(request);
    }

    private static string? ToInputValue(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm");

    private static DateTimeOffset? ParseInputValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static string BuildStateKey(TournamentDto tournament)
        => string.Join(
            "|",
            tournament.Id,
            tournament.RegistrationOpenUtc,
            tournament.RegistrationClosedUtc,
            tournament.StartUtc,
            tournament.EndUtc,
            string.Join(";", tournament.InfoSections.Select(section => $"{section.Id}:{section.SortOrder}:{section.Header}:{section.ContentMarkdown}")));

    private sealed class EditableTournamentInfoSection(Guid id, string header, string contentMarkdown)
    {
        public Guid Id { get; } = id;
        public string Header { get; set; } = header;
        public string ContentMarkdown { get; set; } = contentMarkdown;
    }
}

public sealed record TournamentContentSaveRequest(
    DateTimeOffset? RegistrationOpenUtc,
    DateTimeOffset? RegistrationClosedUtc,
    DateTimeOffset? StartUtc,
    DateTimeOffset? EndUtc,
    IReadOnlyList<TournamentInfoSectionDto> InfoSections);
