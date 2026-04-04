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
        public string CoverageType { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public List<ClaimResponse> Claims { get; set; } = new();
    }
}
