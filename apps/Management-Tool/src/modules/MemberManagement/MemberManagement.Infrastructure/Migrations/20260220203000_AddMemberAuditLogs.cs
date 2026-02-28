using System;
using MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Membermanagement.Infrastructure.Migrations;

[DbContext(typeof(MemberManagementDbContext))]
[Migration("20260220203000_AddMemberAuditLogs")]
public partial class AddMemberAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MemberAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ActionType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                OldValuesJson = table.Column<string>(type: "text", nullable: true),
                NewValuesJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemberAuditLogs", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MemberAuditLogs");
    }
}
