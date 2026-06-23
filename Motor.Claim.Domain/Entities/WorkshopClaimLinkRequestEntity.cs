using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motor.Claim.Domain.Entities
{
    public class WorkshopClaimLinkRequestEntity
    {
        public DateTime CreatedAt { get; set; }

        [Key]
        public Guid RequestId { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public Guid WorkshopId { get; set; }

        [Required]
        public DateTime ArrivalDate { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? WorkshopReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? CustomerResponseNote { get; set; }
        public DateTime? RespondedAt { get; set; }

        [ForeignKey("ClaimId")]
        public ClaimEntity Claim { get; set; } = null!;

        [ForeignKey("WorkshopId")]
        public WorkshopEntity Workshop { get; set; } = null!;
    }
}
