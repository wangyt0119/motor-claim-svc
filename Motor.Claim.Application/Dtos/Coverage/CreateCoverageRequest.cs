using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Coverage
{
    public class CreateCoverageRequest
    {
        [Required]
        public string InsuredPersonName { get; set; }
        
        [Required]
        public string AuthorizedDriver { get; set; }

        [Required]
        public string VehicleNo { get; set; }

        [Required]
        public string CoverageType { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public decimal? CoverageLimitAmount { get; set; }

    }
}
