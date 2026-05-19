using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    public partial class AddCoverageLimitAndEstimatePayables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoverageLimitAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 50000m);

            migrationBuilder.AddColumn<decimal>(
                name: "UsedClaimAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingCoverageAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 50000m);

            migrationBuilder.AddColumn<decimal>(
                name: "InsurancePayableAmount",
                table: "WorkshopRepairEstimates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerPayableAmount",
                table: "WorkshopRepairEstimates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPartialCoverage",
                table: "WorkshopRepairEstimates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverageLimitAmount",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "UsedClaimAmount",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "RemainingCoverageAmount",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "InsurancePayableAmount",
                table: "WorkshopRepairEstimates");

            migrationBuilder.DropColumn(
                name: "CustomerPayableAmount",
                table: "WorkshopRepairEstimates");

            migrationBuilder.DropColumn(
                name: "IsPartialCoverage",
                table: "WorkshopRepairEstimates");
        }
    }
}
