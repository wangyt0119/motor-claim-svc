using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Domain.Entities
{
    public class ClaimEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid ClaimId { get; set; }

        [Required]
        public Guid UserId { get; set; }

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

        public string Status { get; set; } = "Pending";

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }

        [ForeignKey("CoverageId")]
        public CoverageEntity Coverage { get; set; }
    }
}
