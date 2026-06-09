using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAndLineItemPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceBankAccountPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Iban = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Bic = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Blz = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    AccountHolder = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceBankAccountPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItemCollectionPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItemCollectionPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItemPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 12, scale: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItemPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItemCollectionPresetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionPresetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItemCollectionPresetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItemCollectionPresetItems_InvoiceLineItemCollectionPresets_CollectionPresetId",
                        column: x => x.CollectionPresetId,
                        principalTable: "InvoiceLineItemCollectionPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBankAccountPresets_Label",
                table: "InvoiceBankAccountPresets",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItemCollectionPresetItems_CollectionPresetId_SortOrder",
                table: "InvoiceLineItemCollectionPresetItems",
                columns: new[] { "CollectionPresetId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItemCollectionPresets_Label",
                table: "InvoiceLineItemCollectionPresets",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItemPresets_Label",
                table: "InvoiceLineItemPresets",
                column: "Label",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceBankAccountPresets");

            migrationBuilder.DropTable(
                name: "InvoiceLineItemCollectionPresetItems");

            migrationBuilder.DropTable(
                name: "InvoiceLineItemPresets");

            migrationBuilder.DropTable(
                name: "InvoiceLineItemCollectionPresets");
        }
    }
}
