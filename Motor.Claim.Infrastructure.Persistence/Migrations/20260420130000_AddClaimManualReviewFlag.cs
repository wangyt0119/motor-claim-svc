using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    public partial class AddClaimManualReviewFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFlaggedForManualReview",
                table: "Claims",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualReviewFlagReason",
                table: "Claims",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFlaggedForManualReview",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ManualReviewFlagReason",
                table: "Claims");
        }
    }
}
