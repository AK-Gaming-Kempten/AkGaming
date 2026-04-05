using System;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MemberManagementDbContext))]
    [Migration("20260405112000_AddMembershipDueReminderTracking")]
    public partial class AddMembershipDueReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReminderSentAt",
                table: "MembershipDues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReminderSendStatus",
                table: "MembershipDues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "MembershipDues");

            migrationBuilder.DropColumn(
                name: "LastReminderSendStatus",
                table: "MembershipDues");
        }
    }
}
