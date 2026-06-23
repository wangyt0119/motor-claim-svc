namespace Motor.Claim.Application.Dtos.Workshop
{
    public class WorkshopClaimLinkRequestResponse
    {
        public Guid RequestId { get; set; }
        public Guid ClaimId { get; set; }
        public Guid WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public string WorkshopState { get; set; } = string.Empty;
        public DateTime ArrivalDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? WorkshopReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string? CustomerResponseNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
