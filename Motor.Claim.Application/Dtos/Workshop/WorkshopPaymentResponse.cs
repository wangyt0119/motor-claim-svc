namespace Motor.Claim.Application.Dtos.Workshop
{
    public class WorkshopPaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid EstimateId { get; set; }
        public Guid ClaimId { get; set; }
        public Guid WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MYR";
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ApprovalSource { get; set; } = string.Empty;
        public string? ProviderReference { get; set; }
        public string? BankNameSnapshot { get; set; }
        public string? BankAccountNumberSnapshot { get; set; }
        public string? BankAccountHolderNameSnapshot { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
