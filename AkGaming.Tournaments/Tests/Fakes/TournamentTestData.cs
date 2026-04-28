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

    public static MediaAsset AddMediaAsset(InMemoryStore store)
    {
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            ContentType = "image/png",
            OriginalFileName = "game.png",
            Content = [1, 2, 3],
            SizeBytes = 1024
        };

        store.MediaAssets.Add(asset);
        return asset;
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
        TournamentStatus status = TournamentStatus.RegistrationOpen,
        int minimumPlayers = 1,
        int maximumPlayers = 99,
        int? maximumPlayerRankRating = null,
        int? maximumTeamAverageRankRating = null)
    {
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            Status = status
        };
        tournament.RegistrationRules.Add(new MinPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 0, Value = minimumPlayers });
        tournament.RegistrationRules.Add(new MaxPlayersPerTeamRegistrationRule { Id = Guid.NewGuid(), SortOrder = 1, Value = maximumPlayers });

        var sortOrder = 2;
        if (maximumPlayerRankRating is int playerRating)
        {
            tournament.RegistrationRules.Add(new MaxPlayerRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = sortOrder++, Value = playerRating });
        }

        if (maximumTeamAverageRankRating is int averageRating)
        {
            tournament.RegistrationRules.Add(new MaxTeamAverageRankRatingRegistrationRule { Id = Guid.NewGuid(), SortOrder = sortOrder, Value = averageRating });
        }

        store.Tournaments.Add(tournament);
        return tournament;
    }

    public static PlayerProfile AddUserProfile(
        InMemoryStore store,
        string userId = MemberId,
        string gameId = GameId,
        string name = "Member Jungle",
        int? rankRating = null)
    {
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Name = name,
            RankRating = rankRating,
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
        string? gameId = null,
        int? rankRating = null)
    {
        var profile = new PlayerProfile
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            GameId = gameId ?? team.GameId,
            Name = name,
            RankRating = rankRating,
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
