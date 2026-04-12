using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Dtos.Stp
{
    public class StpValidationResultDto
    {
        public StpStatus STPStatus { get; set; } = StpStatus.Pending;
        public bool IsApproved { get; set; }
        public bool IsDocumentComplete { get; set; }
        public bool IsIdentityMatched { get; set; }
        public bool IsVehicleMatched { get; set; }
        public bool IsPoliceReportMatched { get; set; }
        public bool IsDrivingLicenseMatched { get; set; }
        public bool AreEvidenceImagesPresent { get; set; }
        public List<string> Reasons { get; set; } = new();
        public List<OcrDocumentDiagnosticDto> DocumentDiagnostics { get; set; } = new();
    }
}
