using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Membermanagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MembershipApplicationRequest",
                table: "MembershipApplicationRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberLinkingRequest",
                table: "MemberLinkingRequest");

            migrationBuilder.RenameTable(
                name: "MembershipApplicationRequest",
                newName: "MembershipApplicationRequests");

            migrationBuilder.RenameTable(
                name: "MemberLinkingRequest",
                newName: "MemberLinkingRequests");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MembershipApplicationRequests",
                table: "MembershipApplicationRequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberLinkingRequests",
                table: "MemberLinkingRequests",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MembershipApplicationRequests",
                table: "MembershipApplicationRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberLinkingRequests",
                table: "MemberLinkingRequests");

            migrationBuilder.RenameTable(
                name: "MembershipApplicationRequests",
                newName: "MembershipApplicationRequest");

            migrationBuilder.RenameTable(
                name: "MemberLinkingRequests",
                newName: "MemberLinkingRequest");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MembershipApplicationRequest",
                table: "MembershipApplicationRequest",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberLinkingRequest",
                table: "MemberLinkingRequest",
                column: "Id");
        }
    }
}
