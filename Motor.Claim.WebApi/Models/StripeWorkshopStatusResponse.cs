namespace Motor.Claim.WebApi.Models
{
    public class StripeWorkshopStatusResponse
    {
        public Guid WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public string? StripeConnectedAccountId { get; set; }
        public string? StripeOnboardingStatus { get; set; }
        public bool StripeChargesEnabled { get; set; }
        public bool StripePayoutsEnabled { get; set; }
        public DateTime? StripeLastSyncedAt { get; set; }
        public bool IsConnected => !string.IsNullOrWhiteSpace(StripeConnectedAccountId);
    }
}
