using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motor.Claim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workshops",
                columns: table => new
                {
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankAccountHolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPanelWorkshop = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workshops", x => x.WorkshopId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdType = table.Column<int>(type: "int", nullable: false),
                    NRIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssueCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileCountry = table.Column<int>(type: "int", nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMaybankGroupEmployee = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "WorkshopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Coverages",
                columns: table => new
                {
                    CoverageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuredPersonName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorizedDriver = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coverages", x => x.CoverageId);
                    table.ForeignKey(
                        name: "FK_Coverages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoverageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllClaimType = table.Column<int>(type: "int", nullable: false),
                    MotorClaimType = table.Column<int>(type: "int", nullable: true),
                    IncidentDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PoliceReportDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleOwnershipCertificateDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentityDocumentFront = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdentityDocumentBack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrivingLicenseFront = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrivingLicenseBack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleDamageFrontLeftDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleDamageFrontRightDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleDamageRearLeftDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleDamageRearRightDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    STPStatus = table.Column<int>(type: "int", nullable: false),
                    IsSTPApproved = table.Column<bool>(type: "bit", nullable: false),
                    ValidationResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfficerDecisionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerResponseNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_Claims_Coverages_CoverageId",
                        column: x => x.CoverageId,
                        principalTable: "Coverages",
                        principalColumn: "CoverageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Claims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopAppointments",
                columns: table => new
                {
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeSlotStart = table.Column<TimeSpan>(type: "time", nullable: false),
                    TimeSlotEnd = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopAppointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_WorkshopAppointments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "ClaimId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkshopAppointments_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "WorkshopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopRepairEstimates",
                columns: table => new
                {
                    EstimateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceiptOrQuotationDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportingDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsStpApproved = table.Column<bool>(type: "bit", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopRepairEstimates", x => x.EstimateId);
                    table.ForeignKey(
                        name: "FK_WorkshopRepairEstimates_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "ClaimId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkshopRepairEstimates_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "WorkshopId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_CoverageId",
                table: "Claims",
                column: "CoverageId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_UserId",
                table: "Claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coverages_UserId",
                table: "Coverages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkshopId",
                table: "Users",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopAppointments_ClaimId",
                table: "WorkshopAppointments",
                column: "ClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopAppointments_WorkshopId",
                table: "WorkshopAppointments",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopRepairEstimates_ClaimId",
                table: "WorkshopRepairEstimates",
                column: "ClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopRepairEstimates_WorkshopId",
                table: "WorkshopRepairEstimates",
                column: "WorkshopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkshopAppointments");

            migrationBuilder.DropTable(
                name: "WorkshopRepairEstimates");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Coverages");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workshops");
        }
    }
}
