using AkGaming.Tournaments.Application.Exceptions;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Domain.Enums;
using AkGaming.Tournaments.Tests.Fakes;

namespace AkGaming.Tournaments.Tests.Application;

public sealed class TeamManagementServiceTests
{
    private InMemoryStore Store { get; set; } = null!;

    private FakeUnitOfWork UnitOfWork { get; set; } = null!;

    private TeamManagementService Service { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Store = new InMemoryStore();
        UnitOfWork = new FakeUnitOfWork();
        Service = new TeamManagementService(
            new InMemoryGameRepository(Store),
            new InMemoryMediaAssetRepository(Store),
            new InMemoryPlayerProfileRepository(Store),
            new InMemoryTeamRepository(Store),
            UnitOfWork);
    }

    [Test]
    [Description("Verifies that only owners can add members and that added members receive the requested role.")]
    public void AddMemberAsync_AddsMemberWhenActorIsOwner()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store);

        // Act
        var updated = Service.AddMemberAsync(team.Id, TournamentTestData.OwnerId, " member-1 ", TeamRoleDto.Editor).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.Memberships, Has.Count.EqualTo(2));
            Assert.That(updated.Memberships.Single(member => member.UserId == TournamentTestData.MemberId).Role, Is.EqualTo(TeamRoleDto.Editor));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that a user cannot be added to the same team twice, even with different casing.")]
    public void AddMemberAsync_RejectsDuplicateMember()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.MemberId, TeamRole.Member)
        ]);

        // Act
        Task Act() => Service.AddMemberAsync(team.Id, TournamentTestData.OwnerId, "MEMBER-1", TeamRoleDto.Member);

        // Assert
        Assert.ThrowsAsync<ConflictException>(Act);
    }

    [Test]
    [Description("Verifies that non-owners cannot manage team membership.")]
    public void AddMemberAsync_RejectsNonOwnerMembershipManagement()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.EditorId, TeamRole.Editor)]);

        // Act
        Task Act() => Service.AddMemberAsync(team.Id, TournamentTestData.EditorId, TournamentTestData.MemberId, TeamRoleDto.Member);

        // Assert
        Assert.ThrowsAsync<ForbiddenException>(Act);
    }

    [Test]
    [Description("Verifies that owners and editors can create and revoke invite keys for their team.")]
    public void CreateInviteKeyAsync_AndRevokeInviteKeyAsync_WorkForEditors()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.EditorId, TeamRole.Editor)
        ]);

        // Act
        var created = Service.CreateInviteKeyAsync(team.Id, TournamentTestData.EditorId, 3).GetAwaiter().GetResult();
        var revoked = Service.RevokeInviteKeyAsync(team.Id, created.Key, TournamentTestData.OwnerId).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(created.RemainingUses, Is.EqualTo(3));
            Assert.That(revoked.RemainingUses, Is.EqualTo(0));
            Assert.That(revoked.RevokedUtc, Is.Not.Null);
        });
    }

    [Test]
    [Description("Verifies that accepting an invite key adds the user as member and decrements remaining uses.")]
    public void AcceptInviteAsync_AddsMemberAndConsumesUse()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.OwnerId, TeamRole.Owner)]);
        var invite = Service.CreateInviteKeyAsync(team.Id, TournamentTestData.OwnerId, 1).GetAwaiter().GetResult();

        // Act
        var accepted = Service.AcceptInviteAsync(team.Id, invite.Key, TournamentTestData.MemberId).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(accepted.RemainingUses, Is.EqualTo(0));
            Assert.That(team.Memberships.Any(member => member.UserId == TournamentTestData.MemberId), Is.True);
        });
    }

    [Test]
    [Description("Verifies that regular members cannot create guest profiles for the team.")]
    public void CreateGuestPlayerProfileAsync_RejectsMember()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.MemberId, TeamRole.Member)
        ]);

        // Act
        Task Act() => Service.CreateGuestPlayerProfileAsync(team.Id, TournamentTestData.MemberId, "Guest Mid");

        // Assert
        Assert.ThrowsAsync<ForbiddenException>(Act);
    }

    [Test]
    [Description("Verifies that owners and editors can create guest profiles and that guest profiles inherit the team's game.")]
    public void CreateGuestPlayerProfileAsync_UsesTeamGameForEditorsAndOwners()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.EditorId, TeamRole.Editor)
        ]);

        // Act
        var profile = Service.CreateGuestPlayerProfileAsync(team.Id, TournamentTestData.EditorId, " Guest Mid ", 1400).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(profile.GameId, Is.EqualTo(TournamentTestData.GameId));
            Assert.That(profile.TeamId, Is.EqualTo(team.Id));
            Assert.That(profile.Name, Is.EqualTo("Guest Mid"));
            Assert.That(profile.RankRating, Is.EqualTo(1400));
            Assert.That(Store.PlayerProfiles.Single().Id, Is.EqualTo(profile.Id));
        });
    }

    [Test]
    [Description("Verifies that creating a team scopes it to a game, trims input, and assigns the creator as owner.")]
    public void CreateTeamAsync_AssignsCreatorAsOwnerForGame()
    {
        // Arrange
        TournamentTestData.AddGame(Store);

        // Act
        var team = Service.CreateTeamAsync(" captain-1 ", " lol ", " AKG Blue ").GetAwaiter().GetResult();

        // Assert
        Assert.That(team.Memberships, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(team.GameId, Is.EqualTo(TournamentTestData.GameId));
            Assert.That(team.Name, Is.EqualTo("AKG Blue"));
            Assert.That(team.Memberships[0].UserId, Is.EqualTo(TournamentTestData.OwnerId));
            Assert.That(team.Memberships[0].Role, Is.EqualTo(TeamRoleDto.Owner));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that creating a team requires a valid user id, game id, team name, and existing game.")]
    public void CreateTeamAsync_RejectsInvalidInput()
    {
        // Arrange
        TournamentTestData.AddGame(Store);

        // Act
        Task BlankUserId() => Service.CreateTeamAsync(" ", TournamentTestData.GameId, "AKG Blue");
        Task BlankGameId() => Service.CreateTeamAsync(TournamentTestData.OwnerId, " ", "AKG Blue");
        Task BlankTeamName() => Service.CreateTeamAsync(TournamentTestData.OwnerId, TournamentTestData.GameId, " ");
        Task UnknownGame() => Service.CreateTeamAsync(TournamentTestData.OwnerId, TournamentTestData.OtherGameId, "AKG Blue");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<ValidationException>(BlankUserId);
            Assert.ThrowsAsync<ValidationException>(BlankGameId);
            Assert.ThrowsAsync<ValidationException>(BlankTeamName);
            Assert.ThrowsAsync<NotFoundException>(UnknownGame);
        });
    }

    [Test]
    [Description("Verifies that the available player pool cannot be requested for a game different from the team's game.")]
    public void GetAvailableProfilesAsync_RejectsGameThatDoesNotMatchTeam()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        TournamentTestData.AddGame(Store, TournamentTestData.OtherGameId, "Valorant");
        var team = TournamentTestData.AddTeam(Store);

        // Act
        Task Act() => Service.GetAvailableProfilesAsync(team.Id, TournamentTestData.OtherGameId);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that the available player pool contains team guest profiles and user profiles for team members in the team's game only.")]
    public void GetAvailableProfilesAsync_ReturnsMemberAndGuestProfilesForTeamGame()
    {
        // Arrange
        TournamentTestData.AddGame(Store);
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.MemberId, TeamRole.Member)
        ]);
        TournamentTestData.AddGuestProfile(Store, team, "Guest Mid");
        TournamentTestData.AddUserProfile(Store, TournamentTestData.MemberId, TournamentTestData.GameId, "Member Jungle");
        TournamentTestData.AddUserProfile(Store, TournamentTestData.MemberId, TournamentTestData.OtherGameId, "Wrong Game");
        TournamentTestData.AddUserProfile(Store, TournamentTestData.OtherUserId, TournamentTestData.GameId, "Wrong User");

        // Act
        var profiles = Service.GetAvailableProfilesAsync(team.Id, TournamentTestData.GameId).GetAwaiter().GetResult();

        // Assert
        Assert.That(profiles.Select(profile => profile.Name), Is.EqualTo(new[] { "Guest Mid", "Member Jungle" }));
    }

    [Test]
    [Description("Verifies that getting an unknown team returns null instead of throwing.")]
    public void GetTeamAsync_ReturnsNullForUnknownTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var team = Service.GetTeamAsync(teamId).GetAwaiter().GetResult();

        // Assert
        Assert.That(team, Is.Null);
    }

    [Test]
    [Description("Verifies that getting a team maps memberships and guest profiles without exposing registrations.")]
    public void GetTeamAsync_ReturnsTeamWithMembershipsAndGuestProfiles()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store);
        TournamentTestData.AddGuestProfile(Store, team, "Guest Mid");

        // Act
        var dto = Service.GetTeamAsync(team.Id).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Id, Is.EqualTo(team.Id));
            Assert.That(dto.Memberships, Has.Count.EqualTo(1));
            Assert.That(dto.GuestPlayerProfiles.Single().Name, Is.EqualTo("Guest Mid"));
        });
    }

    [Test]
    [Description("Verifies that user teams are returned only for teams where the user has a membership.")]
    public void GetTeamsForUserAsync_ReturnsOnlyTeamsForUserMemberships()
    {
        // Arrange
        TournamentTestData.AddTeam(Store, name: "AKG Blue", memberships: [(TournamentTestData.MemberId, TeamRole.Member)]);
        TournamentTestData.AddTeam(Store, name: "AKG Red", memberships: [(TournamentTestData.OwnerId, TeamRole.Owner)]);
        TournamentTestData.AddTeam(Store, name: "AKG Green", memberships: [(TournamentTestData.MemberId, TeamRole.Editor)]);

        // Act
        var teams = Service.GetTeamsForUserAsync(TournamentTestData.MemberId).GetAwaiter().GetResult();

        // Assert
        Assert.That(teams.Select(team => team.Name), Is.EqualTo(new[] { "AKG Blue", "AKG Green" }));
    }

    [Test]
    [Description("Verifies that listing user teams requires a user id.")]
    public void GetTeamsForUserAsync_RejectsBlankUserId()
    {
        // Arrange
        const string userId = " ";

        // Act
        Task Act() => Service.GetTeamsForUserAsync(userId);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that updating a guest profile fails when the profile belongs to another team.")]
    public void UpdateGuestPlayerProfileAsync_RejectsProfileOwnedByAnotherTeam()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, name: "AKG Blue");
        var otherTeam = TournamentTestData.AddTeam(Store, name: "AKG Red");
        var otherProfile = TournamentTestData.AddGuestProfile(Store, otherTeam, "Other Guest");

        // Act
        Task Act() => Service.UpdateGuestPlayerProfileAsync(team.Id, otherProfile.Id, TournamentTestData.OwnerId, "Guest ADC");

        // Assert
        Assert.ThrowsAsync<NotFoundException>(Act);
    }

    [Test]
    [Description("Verifies that owners and editors can update the team logo.")]
    public void UpdateTeamLogoAsync_UpdatesLogoForEditors()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.EditorId, TeamRole.Editor)]);
        var asset = TournamentTestData.AddMediaAsset(Store);

        // Act
        var updated = Service.UpdateTeamLogoAsync(team.Id, TournamentTestData.EditorId, asset.Id).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.LogoAssetId, Is.EqualTo(asset.Id));
            Assert.That(Store.Teams.Single().LogoAssetId, Is.EqualTo(asset.Id));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that owners and editors can rename a team and that input is trimmed before saving.")]
    public void UpdateTeamAsync_UpdatesTeamNameForEditors()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.EditorId, TeamRole.Editor)]);

        // Act
        var updated = Service.UpdateTeamAsync(team.Id, TournamentTestData.EditorId, " AKG Crimson ", null, null).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.Name, Is.EqualTo("AKG Crimson"));
            Assert.That(Store.Teams.Single().Name, Is.EqualTo("AKG Crimson"));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that updating a guest profile changes the live profile and revision timestamp.")]
    public void UpdateGuestPlayerProfileAsync_UpdatesNameAndRevision()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store);
        var profile = TournamentTestData.AddGuestProfile(Store, team, "Guest Mid");
        var previousRevision = profile.LastRevisionUtc;

        // Act
        var updated = Service.UpdateGuestPlayerProfileAsync(team.Id, profile.Id, TournamentTestData.OwnerId, "Guest ADC", 1500).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.Name, Is.EqualTo("Guest ADC"));
            Assert.That(updated.RankRating, Is.EqualTo(1500));
            Assert.That(profile.LastRevisionUtc, Is.GreaterThan(previousRevision));
        });
    }

    [Test]
    [Description("Verifies that owners and editors can delete guest profiles from the team roster.")]
    public void DeleteGuestPlayerProfileAsync_RemovesGuestProfile()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships: [(TournamentTestData.EditorId, TeamRole.Editor)]);
        var profile = TournamentTestData.AddGuestProfile(Store, team, "Guest Mid");

        // Act
        var updated = Service.DeleteGuestPlayerProfileAsync(team.Id, profile.Id, TournamentTestData.EditorId).GetAwaiter().GetResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updated.GuestPlayerProfiles, Is.Empty);
            Assert.That(Store.PlayerProfiles, Does.Not.Contain(profile));
            Assert.That(UnitOfWork.SaveChangesCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    [Description("Verifies that role updates cannot remove the last owner from a team.")]
    public void UpdateMemberRoleAsync_RejectsRemovingLastOwner()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store);

        // Act
        Task Act() => Service.UpdateMemberRoleAsync(team.Id, TournamentTestData.OwnerId, TournamentTestData.OwnerId, TeamRoleDto.Member);

        // Assert
        Assert.ThrowsAsync<ValidationException>(Act);
    }

    [Test]
    [Description("Verifies that role updates fail when the target user is not a team member.")]
    public void UpdateMemberRoleAsync_RejectsUnknownMember()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store);

        // Act
        Task Act() => Service.UpdateMemberRoleAsync(team.Id, TournamentTestData.OwnerId, TournamentTestData.MemberId, TeamRoleDto.Member);

        // Assert
        Assert.ThrowsAsync<NotFoundException>(Act);
    }

    [Test]
    [Description("Verifies that an owner can update an existing member's role.")]
    public void UpdateMemberRoleAsync_UpdatesExistingMemberRole()
    {
        // Arrange
        var team = TournamentTestData.AddTeam(Store, memberships:
        [
            (TournamentTestData.OwnerId, TeamRole.Owner),
            (TournamentTestData.MemberId, TeamRole.Member)
        ]);

        // Act
        var updated = Service.UpdateMemberRoleAsync(team.Id, TournamentTestData.OwnerId, TournamentTestData.MemberId, TeamRoleDto.Editor).GetAwaiter().GetResult();

        // Assert
        Assert.That(updated.Memberships.Single(member => member.UserId == TournamentTestData.MemberId).Role, Is.EqualTo(TeamRoleDto.Editor));
    }
}
