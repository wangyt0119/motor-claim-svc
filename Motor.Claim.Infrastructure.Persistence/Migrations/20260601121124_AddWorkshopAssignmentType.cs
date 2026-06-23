using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkshopAssignmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                table: "WorkshopAppointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "ScheduledAppointment");

            migrationBuilder.AddColumn<string>(
                name: "WorkshopReferenceNumber",
                table: "WorkshopAppointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "WorkshopAppointments");

            migrationBuilder.DropColumn(
                name: "WorkshopReferenceNumber",
                table: "WorkshopAppointments");
        }
    }
}
