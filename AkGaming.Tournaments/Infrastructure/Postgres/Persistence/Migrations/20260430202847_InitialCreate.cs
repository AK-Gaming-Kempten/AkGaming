using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_games_media_assets_LogoAssetId",
                        column: x => x.LogoAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    BannerAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ProfileLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teams_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_media_assets_BannerAssetId",
                        column: x => x.BannerAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_teams_media_assets_LogoAssetId",
                        column: x => x.LogoAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    BannerAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryColor = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RegistrationOpenUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RegistrationClosedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournaments_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tournaments_media_assets_BannerAssetId",
                        column: x => x.BannerAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tournaments_media_assets_LogoAssetId",
                        column: x => x.LogoAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "player_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LogoAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RankRating = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProfileLink = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LastRevisionUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_profiles_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_profiles_media_assets_LogoAssetId",
                        column: x => x.LogoAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_player_profiles_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "team_invite_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RemainingUses = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_invite_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_invite_keys_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_memberships_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_info_sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Header = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_info_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_info_sections_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tournament_registration_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    RuleType = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_registration_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_registration_rules_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roster_player_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RosterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePlayerProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlayerProfileType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourcePlayerProfileLastRevisionUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SnapshotCreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roster_player_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roster_player_snapshots_player_profiles_SourcePlayerProfile~",
                        column: x => x.SourcePlayerProfileId,
                        principalTable: "player_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rosters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rosters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tournament_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true),
                    ActiveRosterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_registrations_rosters_ActiveRosterId",
                        column: x => x.ActiveRosterId,
                        principalTable: "rosters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tournament_registrations_teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tournament_registrations_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_games_LogoAssetId",
                table: "games",
                column: "LogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_GameId",
                table: "player_profiles",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_LogoAssetId",
                table: "player_profiles",
                column: "LogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_TeamId",
                table: "player_profiles",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_player_profiles_UserId",
                table: "player_profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_roster_player_snapshots_RosterId",
                table: "roster_player_snapshots",
                column: "RosterId");

            migrationBuilder.CreateIndex(
                name: "IX_roster_player_snapshots_SourcePlayerProfileId",
                table: "roster_player_snapshots",
                column: "SourcePlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_rosters_TournamentRegistrationId",
                table: "rosters",
                column: "TournamentRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_team_invite_keys_TeamId_Key",
                table: "team_invite_keys",
                columns: new[] { "TeamId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_TeamId_UserId",
                table: "team_memberships",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_BannerAssetId",
                table: "teams",
                column: "BannerAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_GameId",
                table: "teams",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_LogoAssetId",
                table: "teams",
                column: "LogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_info_sections_TournamentId_SortOrder",
                table: "tournament_info_sections",
                columns: new[] { "TournamentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registration_rules_TournamentId_SortOrder",
                table: "tournament_registration_rules",
                columns: new[] { "TournamentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_ActiveRosterId",
                table: "tournament_registrations",
                column: "ActiveRosterId");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_TeamId",
                table: "tournament_registrations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_tournament_registrations_TournamentId_TeamId",
                table: "tournament_registrations",
                columns: new[] { "TournamentId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_BannerAssetId",
                table: "tournaments",
                column: "BannerAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_GameId",
                table: "tournaments",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_LogoAssetId",
                table: "tournaments",
                column: "LogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_Slug",
                table: "tournaments",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_roster_player_snapshots_rosters_RosterId",
                table: "roster_player_snapshots",
                column: "RosterId",
                principalTable: "rosters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rosters_tournament_registrations_TournamentRegistrationId",
                table: "rosters",
                column: "TournamentRegistrationId",
                principalTable: "tournament_registrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_games_media_assets_LogoAssetId",
                table: "games");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_media_assets_BannerAssetId",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_media_assets_LogoAssetId",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_media_assets_BannerAssetId",
                table: "tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_media_assets_LogoAssetId",
                table: "tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_teams_games_GameId",
                table: "teams");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_games_GameId",
                table: "tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_tournament_registrations_teams_TeamId",
                table: "tournament_registrations");

            migrationBuilder.DropForeignKey(
                name: "FK_tournament_registrations_rosters_ActiveRosterId",
                table: "tournament_registrations");

            migrationBuilder.DropTable(
                name: "roster_player_snapshots");

            migrationBuilder.DropTable(
                name: "team_invite_keys");

            migrationBuilder.DropTable(
                name: "team_memberships");

            migrationBuilder.DropTable(
                name: "tournament_info_sections");

            migrationBuilder.DropTable(
                name: "tournament_registration_rules");

            migrationBuilder.DropTable(
                name: "player_profiles");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "rosters");

            migrationBuilder.DropTable(
                name: "tournament_registrations");

            migrationBuilder.DropTable(
                name: "tournaments");
        }
    }
}
