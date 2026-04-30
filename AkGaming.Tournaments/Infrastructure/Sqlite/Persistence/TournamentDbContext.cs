using AkGaming.Tournaments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;

public sealed class TournamentDbContext(DbContextOptions<TournamentDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<RosterPlayerSnapshot> RosterPlayerSnapshots => Set<RosterPlayerSnapshot>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<TeamInviteKey> TeamInviteKeys => Set<TeamInviteKey>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentInfoSection> TournamentInfoSections => Set<TournamentInfoSection>();
    public DbSet<TournamentRegistrationRule> TournamentRegistrationRules => Set<TournamentRegistrationRule>();
    public DbSet<TournamentRegistration> TournamentRegistrations => Set<TournamentRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("media_assets");
            entity.HasKey(mediaAsset => mediaAsset.Id);
            entity.Property(mediaAsset => mediaAsset.ContentType).HasMaxLength(128);
            entity.Property(mediaAsset => mediaAsset.OriginalFileName).HasMaxLength(256);
            entity.Property(mediaAsset => mediaAsset.Content).IsRequired();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(game => game.Id);
            entity.Property(game => game.Id).HasMaxLength(64);
            entity.Property(game => game.Name).HasMaxLength(128);
            entity.HasOne(game => game.LogoAsset)
                .WithMany()
                .HasForeignKey(game => game.LogoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(team => team.Id);
            entity.Property(team => team.GameId).HasMaxLength(64);
            entity.Property(team => team.Name).HasMaxLength(256);
            entity.Property(team => team.PrimaryColor).HasMaxLength(16);
            entity.Property(team => team.ProfileLink).HasMaxLength(1024);
            entity.HasOne(team => team.Game)
                .WithMany()
                .HasForeignKey(team => team.GameId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(team => team.LogoAsset)
                .WithMany()
                .HasForeignKey(team => team.LogoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(team => team.BannerAsset)
                .WithMany()
                .HasForeignKey(team => team.BannerAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(team => team.Memberships)
                .WithOne(membership => membership.Team)
                .HasForeignKey(membership => membership.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(team => team.InviteKeys)
                .WithOne(invite => invite.Team)
                .HasForeignKey(invite => invite.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(team => team.GuestPlayerProfiles)
                .WithOne(playerProfile => playerProfile.Team)
                .HasForeignKey(playerProfile => playerProfile.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(team => team.Registrations)
                .WithOne(registration => registration.Team)
                .HasForeignKey(registration => registration.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMembership>(entity =>
        {
            entity.ToTable("team_memberships");
            entity.HasKey(membership => membership.Id);
            entity.Property(membership => membership.UserId).HasMaxLength(128);
            entity.Property(membership => membership.Role).HasConversion<string>();
            entity.HasIndex(membership => new { membership.TeamId, membership.UserId }).IsUnique();
        });

        modelBuilder.Entity<TeamInviteKey>(entity =>
        {
            entity.ToTable("team_invite_keys");
            entity.HasKey(invite => invite.Id);
            entity.Property(invite => invite.Key).HasMaxLength(128);
            entity.HasIndex(invite => new { invite.TeamId, invite.Key }).IsUnique();
        });

        modelBuilder.Entity<PlayerProfile>(entity =>
        {
            entity.ToTable("player_profiles");
            entity.HasKey(playerProfile => playerProfile.Id);
            entity.Property(playerProfile => playerProfile.GameId).HasMaxLength(64);
            entity.Property(playerProfile => playerProfile.Type).HasConversion<string>();
            entity.Property(playerProfile => playerProfile.Name).HasMaxLength(256);
            entity.Property(playerProfile => playerProfile.UserId).HasMaxLength(128);
            entity.Property(playerProfile => playerProfile.ProfileLink).HasMaxLength(1024);
            entity.HasOne(playerProfile => playerProfile.Game)
                .WithMany()
                .HasForeignKey(playerProfile => playerProfile.GameId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(playerProfile => playerProfile.LogoAsset)
                .WithMany()
                .HasForeignKey(playerProfile => playerProfile.LogoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(playerProfile => playerProfile.UserId);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.ToTable("tournaments");
            entity.HasKey(tournament => tournament.Id);
            entity.Property(tournament => tournament.GameId).HasMaxLength(64);
            entity.Property(tournament => tournament.Slug).HasMaxLength(128);
            entity.Property(tournament => tournament.Name).HasMaxLength(256);
            entity.Property(tournament => tournament.PrimaryColor).HasMaxLength(16);
            entity.Property(tournament => tournament.Status).HasConversion<string>();
            entity.HasIndex(tournament => tournament.Slug).IsUnique();
            entity.HasOne(tournament => tournament.Game)
                .WithMany()
                .HasForeignKey(tournament => tournament.GameId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(tournament => tournament.LogoAsset)
                .WithMany()
                .HasForeignKey(tournament => tournament.LogoAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(tournament => tournament.BannerAsset)
                .WithMany()
                .HasForeignKey(tournament => tournament.BannerAssetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(tournament => tournament.Registrations)
                .WithOne(registration => registration.Tournament)
                .HasForeignKey(registration => registration.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(tournament => tournament.RegistrationRules)
                .WithOne(rule => rule.Tournament)
                .HasForeignKey(rule => rule.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(tournament => tournament.InfoSections)
                .WithOne(section => section.Tournament)
                .HasForeignKey(section => section.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TournamentInfoSection>(entity =>
        {
            entity.ToTable("tournament_info_sections");
            entity.HasKey(section => section.Id);
            entity.Property(section => section.Header).HasMaxLength(256);
            entity.Property(section => section.ContentMarkdown).IsRequired();
            entity.HasIndex(section => new { section.TournamentId, section.SortOrder });
        });

        modelBuilder.Entity<TournamentRegistrationRule>(entity =>
        {
            entity.ToTable("tournament_registration_rules");
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Value).IsRequired();
            entity.HasDiscriminator<string>("RuleType")
                .HasValue<MinPlayersPerTeamRegistrationRule>("MinPlayersPerTeam")
                .HasValue<MaxPlayersPerTeamRegistrationRule>("MaxPlayersPerTeam")
                .HasValue<MaxPlayerRankRatingRegistrationRule>("MaxPlayerRankRating")
                .HasValue<MaxTeamAverageRankRatingRegistrationRule>("MaxTeamAverageRankRating");
            entity.HasIndex(rule => new { rule.TournamentId, rule.SortOrder });
        });

        modelBuilder.Entity<TournamentRegistration>(entity =>
        {
            entity.ToTable("tournament_registrations");
            entity.HasKey(registration => registration.Id);
            entity.Property(registration => registration.Status).HasConversion<string>();
            entity.HasIndex(registration => new { registration.TournamentId, registration.TeamId }).IsUnique();
            entity.HasOne(registration => registration.ActiveRoster)
                .WithMany()
                .HasForeignKey(registration => registration.ActiveRosterId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(registration => registration.Rosters)
                .WithOne(roster => roster.TournamentRegistration)
                .HasForeignKey(roster => roster.TournamentRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Roster>(entity =>
        {
            entity.ToTable("rosters");
            entity.HasKey(roster => roster.Id);
            entity.Property(roster => roster.Status).HasConversion<string>();
            entity.HasMany(roster => roster.PlayerSnapshots)
                .WithOne(snapshot => snapshot.Roster)
                .HasForeignKey(snapshot => snapshot.RosterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RosterPlayerSnapshot>(entity =>
        {
            entity.ToTable("roster_player_snapshots");
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.PlayerProfileType).HasConversion<string>();
            entity.Property(snapshot => snapshot.Name).HasMaxLength(256);
            entity.Property(snapshot => snapshot.UserId).HasMaxLength(128);
            entity.HasOne(snapshot => snapshot.SourcePlayerProfile)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.SourcePlayerProfileId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
