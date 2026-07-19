using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.GeneralMeetings.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialGeneralMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneralMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentAgendaItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingAgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Heading = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    Minutes = table.Column<string>(type: "TEXT", maxLength: 50000, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingAgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAgendaItems_GeneralMeetingAgendaItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GeneralMeetingAgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAgendaItems_GeneralMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "GeneralMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    MembershipStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CheckedOutAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAttendances_GeneralMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "GeneralMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAuditEvents_GeneralMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "GeneralMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingInvitationDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RecipientEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    RecipientName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingInvitationDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingInvitationDispatches_GeneralMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "GeneralMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingProtocolRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Markdown = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FinalizedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingProtocolRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingProtocolRevisions_GeneralMeetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "GeneralMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumSelections = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowResultsWhileOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingBallots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingBallots_GeneralMeetingAgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "GeneralMeetingAgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingAnonymousCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BallotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "BLOB", maxLength: 32, nullable: false),
                    Issued = table.Column<bool>(type: "INTEGER", nullable: false),
                    Used = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingAnonymousCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAnonymousCredentials_GeneralMeetingBallots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "GeneralMeetingBallots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingAnonymousVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BallotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SelectionsJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingAnonymousVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingAnonymousVotes_GeneralMeetingBallots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "GeneralMeetingBallots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingBallotEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BallotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CredentialIssued = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingBallotEntitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingBallotEntitlements_GeneralMeetingBallots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "GeneralMeetingBallots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralMeetingBallotOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BallotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralMeetingBallotOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralMeetingBallotOptions_GeneralMeetingBallots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "GeneralMeetingBallots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAgendaItems_MeetingId_ParentId_Order",
                table: "GeneralMeetingAgendaItems",
                columns: new[] { "MeetingId", "ParentId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAgendaItems_ParentId",
                table: "GeneralMeetingAgendaItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAnonymousCredentials_BallotId_TokenHash",
                table: "GeneralMeetingAnonymousCredentials",
                columns: new[] { "BallotId", "TokenHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAnonymousVotes_BallotId",
                table: "GeneralMeetingAnonymousVotes",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAttendances_MeetingId_MemberId",
                table: "GeneralMeetingAttendances",
                columns: new[] { "MeetingId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingAuditEvents_MeetingId_OccurredAt",
                table: "GeneralMeetingAuditEvents",
                columns: new[] { "MeetingId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingBallotEntitlements_BallotId_MemberId",
                table: "GeneralMeetingBallotEntitlements",
                columns: new[] { "BallotId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingBallotOptions_BallotId",
                table: "GeneralMeetingBallotOptions",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingBallots_AgendaItemId",
                table: "GeneralMeetingBallots",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingInvitationDispatches_MeetingId",
                table: "GeneralMeetingInvitationDispatches",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralMeetingProtocolRevisions_MeetingId_Revision",
                table: "GeneralMeetingProtocolRevisions",
                columns: new[] { "MeetingId", "Revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneralMeetingAnonymousCredentials");

            migrationBuilder.DropTable(
                name: "GeneralMeetingAnonymousVotes");

            migrationBuilder.DropTable(
                name: "GeneralMeetingAttendances");

            migrationBuilder.DropTable(
                name: "GeneralMeetingAuditEvents");

            migrationBuilder.DropTable(
                name: "GeneralMeetingBallotEntitlements");

            migrationBuilder.DropTable(
                name: "GeneralMeetingBallotOptions");

            migrationBuilder.DropTable(
                name: "GeneralMeetingInvitationDispatches");

            migrationBuilder.DropTable(
                name: "GeneralMeetingProtocolRevisions");

            migrationBuilder.DropTable(
                name: "GeneralMeetingBallots");

            migrationBuilder.DropTable(
                name: "GeneralMeetingAgendaItems");

            migrationBuilder.DropTable(
                name: "GeneralMeetings");
        }
    }
}
