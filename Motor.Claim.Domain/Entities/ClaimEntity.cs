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
        public string ReviewStatus { get; set; } = "Pending";
        public StpStatus STPStatus { get; set; } = StpStatus.Pending;
        public bool IsSTPApproved { get; set; }
        public bool IsFlaggedForManualReview { get; set; }
        public string? ManualReviewFlagReason { get; set; }
        public string? ValidationResult { get; set; }
        public string? OfficerDecisionNote { get; set; }
        public string? RequestedItems { get; set; }
        public string? CustomerResponseNote { get; set; }
        public string? ResponseDocuments { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public WorkshopAppointmentEntity? WorkshopAppointment { get; set; }
        public WorkshopRepairEstimateEntity? WorkshopRepairEstimate { get; set; }
        public WorkshopPaymentEntity? WorkshopPayment { get; set; }

        [NotMapped]
        public bool? EmailNotificationSent { get; set; }

        [NotMapped]
        public string? EmailNotificationMessage { get; set; }

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }

        [ForeignKey("CoverageId")]
        public CoverageEntity Coverage { get; set; }
    }
}
