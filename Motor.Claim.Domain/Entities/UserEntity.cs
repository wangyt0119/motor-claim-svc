using Motor.Claim.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Domain.Entities
{
    public class UserEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public IdType IdType { get; set; }

        public string? NRIC { get; set; }

        public string? PassportNo { get; set; }

        public string? IssueCountry { get; set; }

        [Required]
        public MobileCountry MobileCountry { get; set; }

        [Required]
        public string MobileNumber { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public bool IsMaybankGroupEmployee { get; set; }

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public Guid? WorkshopId { get; set; }

        public ICollection<CoverageEntity> Coverages { get; set; } = new List<CoverageEntity>();
        public ICollection<ClaimEntity> Claims { get; set; } = new List<ClaimEntity>();
        public WorkshopEntity? Workshop { get; set; }
    }
}
