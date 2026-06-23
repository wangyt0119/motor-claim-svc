namespace Motor.Claim.Application.Dtos.DamageAssessment
{
    public class DamageAssessmentLineItemResponse
    {
        public string Item { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string DamageType { get; set; } = string.Empty;
        public string RecommendedRepair { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
    }
}
