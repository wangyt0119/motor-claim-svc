using System.ComponentModel.DataAnnotations;

using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Dtos.Claim
{
    public class CreateClaimRequest
    {
        [Required]
        public Guid CoverageId { get; set; }

        [Required]
        public DateTime IncidentDate { get; set; }

        [Required]
        public AllClaimType AllClaimType { get; set; }

        public MotorClaimType? MotorClaimType { get; set; }

        [Required]
        public string IncidentDescription { get; set; } = string.Empty;

        public string? PoliceReportDocument { get; set; }
        public string? VehicleOwnershipCertificateDocument { get; set; }
        public string? IdentityDocumentFront { get; set; }
        public string? IdentityDocumentBack { get; set; }
        public string? DrivingLicenseFront { get; set; }
        public string? DrivingLicenseBack { get; set; }
        public string? VehicleDamageFrontLeftDocument { get; set; }
        public string? VehicleDamageFrontRightDocument { get; set; }
        public string? VehicleDamageRearLeftDocument { get; set; }
        public string? VehicleDamageRearRightDocument { get; set; }
    }
}
