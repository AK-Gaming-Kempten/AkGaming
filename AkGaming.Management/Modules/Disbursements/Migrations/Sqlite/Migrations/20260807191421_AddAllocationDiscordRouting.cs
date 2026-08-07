using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.Disbursements.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationDiscordRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordChannelId",
                table: "DisbursementAllocations",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiscordChannelName",
                table: "DisbursementAllocations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiscordRoleId",
                table: "DisbursementAllocations",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DiscordRoleName",
                table: "DisbursementAllocations",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordChannelId",
                table: "DisbursementAllocations");

            migrationBuilder.DropColumn(
                name: "DiscordChannelName",
                table: "DisbursementAllocations");

            migrationBuilder.DropColumn(
                name: "DiscordRoleId",
                table: "DisbursementAllocations");

            migrationBuilder.DropColumn(
                name: "DiscordRoleName",
                table: "DisbursementAllocations");
        }
    }
}
