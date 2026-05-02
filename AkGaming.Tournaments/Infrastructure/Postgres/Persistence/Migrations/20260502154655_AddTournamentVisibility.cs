using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Tournaments.Infrastructure.Postgres.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE tournaments
                SET "IsVisible" = TRUE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "tournaments");
        }
    }
}
