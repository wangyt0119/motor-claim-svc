using System.ComponentModel.DataAnnotations;

namespace Motor.Claim.Application.Dtos.Workshop
{
    public class SubmitWorkshopRepairEstimateRequest
    {
        [Required]
        public Guid ClaimId { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string? ReceiptOrQuotationDocument { get; set; }

        public List<string> SupportingDocuments { get; set; } = new();
        public string? Remarks { get; set; }
    }
}
