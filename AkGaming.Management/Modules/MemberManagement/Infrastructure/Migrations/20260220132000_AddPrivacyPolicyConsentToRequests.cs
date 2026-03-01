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
        migrationBuilder.Sql("""
            ALTER TABLE "MemberLinkingRequests"
            ADD COLUMN IF NOT EXISTS "PrivacyPolicyAccepted" boolean NOT NULL DEFAULT FALSE;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "MembershipApplicationRequests"
            ADD COLUMN IF NOT EXISTS "PrivacyPolicyAccepted" boolean NOT NULL DEFAULT FALSE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "MemberLinkingRequests"
            DROP COLUMN IF EXISTS "PrivacyPolicyAccepted";
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "MembershipApplicationRequests"
            DROP COLUMN IF EXISTS "PrivacyPolicyAccepted";
            """);
    }
}
