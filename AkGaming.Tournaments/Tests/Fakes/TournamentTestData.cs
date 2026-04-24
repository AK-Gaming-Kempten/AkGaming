using AkGaming.Tournaments.Domain.Entities;
using AkGaming.Tournaments.Domain.Enums;

namespace AkGaming.Tournaments.Tests.Fakes;

internal static class TournamentTestData
{
    public const string GameId = "lol";
    public const string OtherGameId = "valorant";
    public const string OwnerId = "captain-1";
    public const string EditorId = "editor-1";
    public const string MemberId = "member-1";
    public const string OtherUserId = "other-user";

    public static Game AddGame(InMemoryStore store, string id = GameId, string name = "League of Legends")
    {
        var game = new Game { Id = id, Name = name };
        store.Games.Add(game);
        return game;
    }

    public static Team AddTeam(
        InMemoryStore store,
        string gameId = GameId,
        string name = "AKG Blue",
        IEnumerable<(string UserId, TeamRole Role)>? memberships = null)
    {
        var team = new Team
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Memberships = (memberships ?? [(OwnerId, TeamRole.Owner)])
                .Select(member => new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = member.UserId,
                    Role = member.Role
                })
                .ToList()
        };

        store.Teams.Add(team);
        return team;
    }

    public static Tournament AddTournament(
        InMemoryStore store,
        string gameId = GameId,
        string name = "Campus Clash",
        TournamentStatus status = TournamentStatus.RegistrationOpen)
    {
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Status = status
        };

        store.Tournaments.Add(tournament);
        return tournament;
    }

    public static PlayerProfile AddUserProfile(
        InMemoryStore store,
        string userId = MemberId,
        string gameId = GameId,
        string name = "Member Jungle")
    {
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Type = PlayerProfileType.User,
            UserId = userId
        };

        store.PlayerProfiles.Add(profile);
        return profile;
    }

    public static PlayerProfile AddGuestProfile(
        InMemoryStore store,
        Team team,
        string name = "Guest Support",
        string? gameId = null)
    {
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = gameId ?? team.GameId,
            Name = name,
            Type = PlayerProfileType.Guest
        };

        team.GuestPlayerProfiles.Add(profile);
        store.PlayerProfiles.Add(profile);
        return profile;
    }

    public static TournamentRegistration AddRegistration(
        InMemoryStore store,
        Team team,
        Tournament tournament,
        TournamentRegistrationStatus status = TournamentRegistrationStatus.Pending,
        RosterStatus rosterStatus = RosterStatus.Pending,
        int version = 1,
        IReadOnlyCollection<PlayerProfile>? profiles = null)
    {
        var roster = new Roster
        {
            Id = Guid.NewGuid(),
            Version = version,
            Status = rosterStatus,
            PlayerSnapshots = (profiles ?? [])
                .Select(profile => new RosterPlayerSnapshot
                {
                    Id = Guid.NewGuid(),
                    SourcePlayerProfileId = profile.Id,
                    PlayerProfileType = profile.Type,
                    Name = profile.Name,
                    UserId = profile.UserId,
                    SourcePlayerProfileLastRevisionUtc = profile.LastRevisionUtc,
                    SnapshotCreatedUtc = DateTimeOffset.UtcNow
                })
                .ToList()
        };

        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            Team = team,
            TournamentId = tournament.Id,
            Tournament = tournament,
            Status = status,
            Rosters = [roster]
        };

        roster.TournamentRegistrationId = registration.Id;
        if (rosterStatus == RosterStatus.Approved)
        {
            registration.ActiveRosterId = roster.Id;
            registration.ActiveRoster = roster;
        }

        store.Registrations.Add(registration);
        team.Registrations.Add(registration);
        tournament.Registrations.Add(registration);
        return registration;
    }
}
