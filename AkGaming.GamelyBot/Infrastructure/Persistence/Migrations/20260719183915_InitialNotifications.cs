using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.GamelyBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isPostgres = ActiveProvider.Contains("Npgsql", StringComparison.Ordinal);
            var guidType = isPostgres ? "uuid" : "TEXT";
            var timestampType = isPostgres ? "timestamp with time zone" : "TEXT";
            var integerType = isPostgres ? "integer" : "INTEGER";
            var textType = isPostgres ? "text" : "TEXT";

            migrationBuilder.CreateTable(
                name: "NotificationInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    EventId = table.Column<Guid>(type: guidType, nullable: false),
                    Type = table.Column<string>(type: textType, maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: textType, maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: false),
                    SubjectUserId = table.Column<Guid>(type: guidType, nullable: true),
                    DataJson = table.Column<string>(type: textType, nullable: false),
                    Status = table.Column<string>(type: textType, maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: integerType, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: true),
                    LastError = table.Column<string>(type: textType, maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    NotificationInboxItemId = table.Column<Guid>(type: guidType, nullable: false),
                    Kind = table.Column<string>(type: textType, maxLength: 32, nullable: false),
                    Target = table.Column<string>(type: textType, maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: textType, maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: textType, nullable: false),
                    Status = table.Column<string>(type: textType, maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: integerType, nullable: false),
                    ExternalMessageId = table.Column<string>(type: textType, maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: textType, maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: false),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: timestampType, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_NotificationInbox_NotificationInboxItemId",
                        column: x => x.NotificationInboxItemId,
                        principalTable: "NotificationInbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationInboxItemId_Kind",
                table: "NotificationDeliveries",
                columns: new[] { "NotificationInboxItemId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationInbox_EventId",
                table: "NotificationInbox",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationInbox_Status_NextAttemptAtUtc",
                table: "NotificationInbox",
                columns: new[] { "Status", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "NotificationInbox");
        }
    }
}
