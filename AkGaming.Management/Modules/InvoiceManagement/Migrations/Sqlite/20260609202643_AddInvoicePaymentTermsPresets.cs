using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentTermsPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoicePaymentTermsPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Terms = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePaymentTermsPresets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePaymentTermsPresets_Label",
                table: "InvoicePaymentTermsPresets",
                column: "Label",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoicePaymentTermsPresets");
        }
    }
}
