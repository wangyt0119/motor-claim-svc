using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Motor.Claim.Domain.Entities
{
    public class WorkshopPaymentEntity
    {
        [Key]
        public Guid PaymentId { get; set; }

        [Required]
        public Guid EstimateId { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public Guid WorkshopId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "MYR";

        [Required]
        public string Status { get; set; } = "Pending";

        [Required]
        public string Provider { get; set; } = "MockSandbox";

        [Required]
        public string ApprovalSource { get; set; } = "ManualReview";

        public string? ProviderReference { get; set; }
        public string? BankNameSnapshot { get; set; }
        public string? BankAccountNumberSnapshot { get; set; }
        public string? BankAccountHolderNameSnapshot { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [ForeignKey("EstimateId")]
        public WorkshopRepairEstimateEntity Estimate { get; set; } = null!;

        [ForeignKey("ClaimId")]
        public ClaimEntity Claim { get; set; } = null!;

        [ForeignKey("WorkshopId")]
        public WorkshopEntity Workshop { get; set; } = null!;
    }
}
