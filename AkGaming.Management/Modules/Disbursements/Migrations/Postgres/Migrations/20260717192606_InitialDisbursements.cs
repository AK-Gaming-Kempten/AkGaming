using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkGaming.Management.Modules.Disbursements.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialDisbursements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisbursementEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementReimbursements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AdministrativeNote = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaymentInformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodDisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementReimbursements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ShareToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementAllocations_DisbursementEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "DisbursementEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementExpenseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReimbursementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IncurredOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementExpenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementExpenseItems_DisbursementReimbursements_Reimbur~",
                        column: x => x.ReimbursementId,
                        principalTable: "DisbursementReimbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementAllocationApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaymentInformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodDisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementAllocationApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementAllocationApplications_DisbursementAllocations_~",
                        column: x => x.AllocationId,
                        principalTable: "DisbursementAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementReceipts_DisbursementExpenseItems_ExpenseItemId",
                        column: x => x.ExpenseItemId,
                        principalTable: "DisbursementExpenseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisbursementAllocationApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementAllocationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementAllocationApprovals_DisbursementAllocationAppli~",
                        column: x => x.ApplicationId,
                        principalTable: "DisbursementAllocationApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementAllocationApplications_AllocationId",
                table: "DisbursementAllocationApplications",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementAllocationApplications_ApplicantUserId",
                table: "DisbursementAllocationApplications",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementAllocationApprovals_ApplicationId_ApproverUserId",
                table: "DisbursementAllocationApprovals",
                columns: new[] { "ApplicationId", "ApproverUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementAllocations_EventId",
                table: "DisbursementAllocations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementAllocations_ShareToken",
                table: "DisbursementAllocations",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementExpenseItems_ReimbursementId",
                table: "DisbursementExpenseItems",
                column: "ReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementReceipts_ExpenseItemId",
                table: "DisbursementReceipts",
                column: "ExpenseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementReimbursements_UserId",
                table: "DisbursementReimbursements",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisbursementAllocationApprovals");

            migrationBuilder.DropTable(
                name: "DisbursementReceipts");

            migrationBuilder.DropTable(
                name: "DisbursementAllocationApplications");

            migrationBuilder.DropTable(
                name: "DisbursementExpenseItems");

            migrationBuilder.DropTable(
                name: "DisbursementAllocations");

            migrationBuilder.DropTable(
                name: "DisbursementReimbursements");

            migrationBuilder.DropTable(
                name: "DisbursementEvents");
        }
    }
}
