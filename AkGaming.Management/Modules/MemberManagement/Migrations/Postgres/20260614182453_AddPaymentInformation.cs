using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Migrations;

public partial class AddPaymentInformation : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "PaymentInformation",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                PayPalEmail = table.Column<string>(type: "text", nullable: true),
                AccountHolder = table.Column<string>(type: "text", nullable: true),
                Iban = table.Column<string>(type: "text", nullable: true),
                Bic = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentInformation", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentInformation_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentInformation_MemberId",
            table: "PaymentInformation",
            column: "MemberId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(name: "PaymentInformation");
    }
}
