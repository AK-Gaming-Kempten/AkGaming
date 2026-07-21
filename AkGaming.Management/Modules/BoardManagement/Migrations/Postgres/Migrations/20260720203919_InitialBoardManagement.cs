using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.BoardManagement.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialBoardManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduleVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardNotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardNotificationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardAgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardAgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardAgendaItems_BoardMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "BoardMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetingAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduleVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetingAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardMeetingAvailabilities_BoardMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "BoardMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetingRescheduleProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedByDisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetingRescheduleProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardMeetingRescheduleProposals_BoardMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "BoardMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardAgendaItems_MeetingId_Order",
                table: "BoardAgendaItems",
                columns: new[] { "MeetingId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_BoardMeetingAvailabilities_MeetingId_UserId",
                table: "BoardMeetingAvailabilities",
                columns: new[] { "MeetingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardMeetingRescheduleProposals_MeetingId",
                table: "BoardMeetingRescheduleProposals",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardNotificationOutbox_EventId",
                table: "BoardNotificationOutbox",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardNotificationOutbox_ProcessedAtUtc_NextAttemptAtUtc_Cre~",
                table: "BoardNotificationOutbox",
                columns: new[] { "ProcessedAtUtc", "NextAttemptAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardAgendaItems");

            migrationBuilder.DropTable(
                name: "BoardMeetingAvailabilities");

            migrationBuilder.DropTable(
                name: "BoardMeetingRescheduleProposals");

            migrationBuilder.DropTable(
                name: "BoardNotificationOutbox");

            migrationBuilder.DropTable(
                name: "BoardMeetings");
        }
    }
}
