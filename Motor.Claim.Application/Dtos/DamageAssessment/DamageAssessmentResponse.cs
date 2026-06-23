namespace Motor.Claim.Application.Dtos.DamageAssessment
{
    public class DamageAssessmentResponse
    {
        public DamageAssessmentCoverageEligibilityResponse CoverageEligibility { get; set; } = new();
        public string DamageSummary { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public decimal EstimatedRepairCost { get; set; }
        public decimal InsurancePayableAmount { get; set; }
        public decimal CustomerPayableAmount { get; set; }
        public bool IsPartialCoverage { get; set; }
        public decimal ConfidenceScore { get; set; }
        public List<DamageAssessmentLineItemResponse> LineItems { get; set; } = new();
        public List<string> DetectedDamageAreas { get; set; } = new();
        public List<string> SafetyNotes { get; set; } = new();
        public string Disclaimer { get; set; } = string.Empty;
        public string? RawAiResponse { get; set; }
    }
}
