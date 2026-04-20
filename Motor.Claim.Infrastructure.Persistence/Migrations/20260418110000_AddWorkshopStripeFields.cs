using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    public partial class AddWorkshopStripeFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StripeChargesEnabled",
                table: "Workshops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectedAccountId",
                table: "Workshops",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeLastSyncedAt",
                table: "Workshops",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeOnboardingStatus",
                table: "Workshops",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripePayoutsEnabled",
                table: "Workshops",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeChargesEnabled",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAccountId",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "StripeLastSyncedAt",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "StripeOnboardingStatus",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "StripePayoutsEnabled",
                table: "Workshops");
        }
    }
}
