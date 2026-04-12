using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motor.Claim.Domain.Entities
{
    public class WorkshopRepairEstimateEntity
    {
        public Guid EstimateId { get; set; }
        public DateTime SubmittedAt { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public Guid WorkshopId { get; set; }

        [Required]
        public Guid SubmittedByUserId { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }
        public string? ReceiptOrQuotationDocument { get; set; }
        public string? SupportingDocuments { get; set; }
        public string? Remarks { get; set; }

        [Required]
        public string Status { get; set; } = "Submitted";

        [Required]
        public string ReviewMode { get; set; } = "ManualReview";

        public bool IsStpApproved { get; set; }

        public string? ReviewNote { get; set; }
        public string? RequestedItems { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("ClaimId")]
        public ClaimEntity Claim { get; set; } = null!;

        [ForeignKey("WorkshopId")]
        public WorkshopEntity Workshop { get; set; } = null!;
    }
}
