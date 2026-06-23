using Motor.Claim.Application.Dtos.Coverage;

namespace Motor.Claim.Application.Dtos.DamageAssessment
{
    public class DamageAssessmentCoverageEligibilityResponse
    {
        public bool IsEligible { get; set; }
        public string Message { get; set; } = string.Empty;
        public CoverageResponse? Coverage { get; set; }
    }
}
