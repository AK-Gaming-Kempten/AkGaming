using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Users" AS u
                SET "Username" = COALESCE(
                    (
                        SELECT LEFT(BTRIM(el."ProviderUsername"), 100)
                        FROM "ExternalLogins" AS el
                        WHERE el."UserId" = u."Id"
                          AND el."Provider" = 'discord'
                          AND el."ProviderUsername" IS NOT NULL
                          AND BTRIM(el."ProviderUsername") <> ''
                        ORDER BY el."LinkedAtUtc" DESC
                        LIMIT 1
                    ),
                    LEFT(u."Email", 100)
                )
                WHERE u."Username" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");
        }
    }
}
