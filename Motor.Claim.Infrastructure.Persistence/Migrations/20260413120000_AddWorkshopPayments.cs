using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    public partial class AddWorkshopPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkshopPayments",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApprovalSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankNameSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumberSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountHolderNameSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopPayments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_WorkshopPayments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "ClaimId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkshopPayments_WorkshopRepairEstimates_EstimateId",
                        column: x => x.EstimateId,
                        principalTable: "WorkshopRepairEstimates",
                        principalColumn: "EstimateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkshopPayments_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "WorkshopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopPayments_ClaimId",
                table: "WorkshopPayments",
                column: "ClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopPayments_EstimateId",
                table: "WorkshopPayments",
                column: "EstimateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopPayments_WorkshopId",
                table: "WorkshopPayments",
                column: "WorkshopId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkshopPayments");
        }
    }
}
