using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Migrations;

[DbContext(typeof(MemberManagementDbContext))]
[Migration("20260220132000_AddPrivacyPolicyConsentToRequests")]
public partial class AddPrivacyPolicyConsentToRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PrivacyPolicyAccepted",
            table: "MemberLinkingRequests",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "PrivacyPolicyAccepted",
            table: "MembershipApplicationRequests",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PrivacyPolicyAccepted",
            table: "MemberLinkingRequests");

        migrationBuilder.DropColumn(
            name: "PrivacyPolicyAccepted",
            table: "MembershipApplicationRequests");
    }
}
