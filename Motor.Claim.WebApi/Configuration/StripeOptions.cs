namespace Motor.Claim.WebApi.Configuration
{
    public class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string RefreshUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public bool UseSandbox { get; set; } = true;
    }
}
