using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.Disbursements.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddExactPaymentMethodSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodAccountHolder",
                table: "DisbursementReimbursements",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodBic",
                table: "DisbursementReimbursements",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodIban",
                table: "DisbursementReimbursements",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodPayPalEmail",
                table: "DisbursementReimbursements",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodAccountHolder",
                table: "DisbursementAllocationApplications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodBic",
                table: "DisbursementAllocationApplications",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodIban",
                table: "DisbursementAllocationApplications",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodPayPalEmail",
                table: "DisbursementAllocationApplications",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethodAccountHolder",
                table: "DisbursementReimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentMethodBic",
                table: "DisbursementReimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentMethodIban",
                table: "DisbursementReimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentMethodPayPalEmail",
                table: "DisbursementReimbursements");

            migrationBuilder.DropColumn(
                name: "PaymentMethodAccountHolder",
                table: "DisbursementAllocationApplications");

            migrationBuilder.DropColumn(
                name: "PaymentMethodBic",
                table: "DisbursementAllocationApplications");

            migrationBuilder.DropColumn(
                name: "PaymentMethodIban",
                table: "DisbursementAllocationApplications");

            migrationBuilder.DropColumn(
                name: "PaymentMethodPayPalEmail",
                table: "DisbursementAllocationApplications");
        }
    }
}
