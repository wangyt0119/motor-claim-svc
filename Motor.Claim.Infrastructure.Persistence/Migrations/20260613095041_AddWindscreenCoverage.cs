using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWindscreenCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WindscreenCoverageLimitAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WindscreenRemainingCoverageAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WindscreenUsedClaimAmount",
                table: "Coverages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WindscreenCoverageLimitAmount",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "WindscreenRemainingCoverageAmount",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "WindscreenUsedClaimAmount",
                table: "Coverages");
        }
    }
}
