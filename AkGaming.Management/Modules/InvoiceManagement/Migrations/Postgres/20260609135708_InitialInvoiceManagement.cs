using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.InvoiceManagement.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialInvoiceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoicePartyPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Street = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePartyPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IntroText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BodyText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PaymentTerms = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClosingText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SignatureName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Greeting = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceBankDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Iban = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Bic = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Blz = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AccountHolder = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceBankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceBankDetails_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Street = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceParties_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBankDetails_InvoiceId",
                table: "InvoiceBankDetails",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId_SortOrder",
                table: "InvoiceLineItems",
                columns: new[] { "InvoiceId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceParties_InvoiceId_Role",
                table: "InvoiceParties",
                columns: new[] { "InvoiceId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePartyPresets_Label",
                table: "InvoicePartyPresets",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceBankDetails");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "InvoiceParties");

            migrationBuilder.DropTable(
                name: "InvoicePartyPresets");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
