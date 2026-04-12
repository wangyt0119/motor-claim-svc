namespace Motor.Claim.Application.Dtos.Workshop
{
    public class WorkshopRepairEstimateResponse
    {
        public Guid EstimateId { get; set; }
        public Guid ClaimId { get; set; }
        public Guid WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public Guid SubmittedByUserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReceiptOrQuotationDocument { get; set; }
        public List<string> SupportingDocuments { get; set; } = new();
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReviewMode { get; set; } = string.Empty;
        public bool IsStpApproved { get; set; }
        public string? ReviewNote { get; set; }
        public string? RequestedItems { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
