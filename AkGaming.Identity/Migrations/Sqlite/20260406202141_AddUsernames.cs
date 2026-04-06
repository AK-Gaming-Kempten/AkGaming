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
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Users
                SET Username = COALESCE(
                    (
                        SELECT substr(trim(ProviderUsername), 1, 100)
                        FROM ExternalLogins
                        WHERE ExternalLogins.UserId = Users.Id
                          AND ExternalLogins.Provider = 'discord'
                          AND ExternalLogins.ProviderUsername IS NOT NULL
                          AND trim(ExternalLogins.ProviderUsername) <> ''
                        ORDER BY ExternalLogins.LinkedAtUtc DESC
                        LIMIT 1
                    ),
                    substr(Email, 1, 100)
                )
                WHERE Username = '';
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
