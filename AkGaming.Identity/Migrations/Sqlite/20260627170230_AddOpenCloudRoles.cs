using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenCloudRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenCloudRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenCloudRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleOpenCloudRoles",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpenCloudRoleId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleOpenCloudRoles", x => new { x.RoleId, x.OpenCloudRoleId });
                    table.ForeignKey(
                        name: "FK_RoleOpenCloudRoles_OpenCloudRoles_OpenCloudRoleId",
                        column: x => x.OpenCloudRoleId,
                        principalTable: "OpenCloudRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleOpenCloudRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenCloudRoles_Key",
                table: "OpenCloudRoles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleOpenCloudRoles_OpenCloudRoleId",
                table: "RoleOpenCloudRoles",
                column: "OpenCloudRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleOpenCloudRoles");

            migrationBuilder.DropTable(
                name: "OpenCloudRoles");
        }
    }
}
