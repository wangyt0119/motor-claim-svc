using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Dtos.Claim
{
    public class ClaimResponse
    {
        public Guid ClaimId { get; set; }
        public Guid UserId { get; set; }
        public Guid CoverageId { get; set; }
        public DateTime IncidentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public AllClaimType AllClaimType { get; set; }
        public MotorClaimType? MotorClaimType { get; set; }
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
        public string Status { get; set; } = string.Empty;
    }
}
