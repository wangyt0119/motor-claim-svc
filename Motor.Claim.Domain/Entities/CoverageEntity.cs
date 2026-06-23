using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motor.Claim.Domain.Entities
{
    public class CoverageEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid CoverageId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string InsuredPersonName { get; set; }
        
        [Required]
        public string AuthorizedDriver { get; set; }

        [Required]
        public string VehicleNo { get; set; }

        [Required]
        public string VehicleMake { get; set; }

        [Required]
        public string VehicleModel { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string ModelType { get; set; }

        [Required]
        public string CoverageType { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public decimal CoverageLimitAmount { get; set; }

        [Required]
        public decimal UsedClaimAmount { get; set; }

        [Required]
        public decimal RemainingCoverageAmount { get; set; }

        [Required]
        public decimal WindscreenCoverageLimitAmount { get; set; }

        [Required]
        public decimal WindscreenUsedClaimAmount { get; set; }

        [Required]
        public decimal WindscreenRemainingCoverageAmount { get; set; }

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }

        public ICollection<ClaimEntity> Claims { get; set; } = new List<ClaimEntity>();
    }
}
