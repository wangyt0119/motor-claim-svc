namespace Motor.Claim.WebApi.Models
{
    public class CreateStripeConnectedAccountResponse
    {
        public string StripeConnectedAccountId { get; set; } = string.Empty;
        public string StripeOnboardingStatus { get; set; } = string.Empty;
        public bool StripeChargesEnabled { get; set; }
        public bool StripePayoutsEnabled { get; set; }
        public DateTime? StripeLastSyncedAt { get; set; }
    }
}
