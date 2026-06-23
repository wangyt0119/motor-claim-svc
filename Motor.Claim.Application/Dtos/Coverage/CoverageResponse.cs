using Motor.Claim.Application.Dtos.Claim;

namespace Motor.Claim.Application.Dtos.Coverage
{
    public class CoverageResponse
    {
        public Guid CoverageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string InsuredPersonName { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public string VehicleMake { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public int Year { get; set; }
        public string ModelType { get; set; } = string.Empty;
        public string CoverageType { get; set; } = string.Empty;
        public string? AuthorizedDriver { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal CoverageLimitAmount { get; set; }
        public decimal UsedClaimAmount { get; set; }
        public decimal RemainingCoverageAmount { get; set; }
        public decimal WindscreenCoverageLimitAmount { get; set; }
        public decimal WindscreenUsedClaimAmount { get; set; }
        public decimal WindscreenRemainingCoverageAmount { get; set; }
        public List<ClaimResponse> Claims { get; set; } = new();
    }
}
