using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Workshop
{
    public class CreateWorkshopClaimLinkRequest
    {
        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public DateTime ArrivalDate { get; set; }

        [MaxLength(100)]
        public string? WorkshopReferenceNumber { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
