namespace Motor.Claim.WebApi.Models
{
    public class CreateStripeOnboardingLinkResponse
    {
        public string Url { get; set; } = string.Empty;
        public DateTime? StripeLastSyncedAt { get; set; }
    }
}
