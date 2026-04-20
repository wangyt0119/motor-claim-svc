using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.WebApi.Configuration;
using Microsoft.Extensions.Options;
using Stripe;

namespace Motor.Claim.WebApi.Services
{
    public class WorkshopPaymentProviderResolver : IWorkshopPaymentProviderResolver
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkshopPaymentProviderResolver> _logger;
        private readonly StripeOptions _stripeOptions;

        public WorkshopPaymentProviderResolver(
            IConfiguration configuration,
            ILogger<WorkshopPaymentProviderResolver> logger,
            IOptions<StripeOptions> stripeOptions)
        {
            _configuration = configuration;
            _logger = logger;
            _stripeOptions = stripeOptions.Value;
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
        }

        public async Task<WorkshopPaymentProviderResolution> ResolveAsync(WorkshopRepairEstimateEntity estimate)
        {
            var provider = _configuration["Payments:Provider"]?.Trim();
            var workshop = estimate.Workshop ?? throw new ArgumentException("Workshop details are required before creating provider payment.");

            if (string.Equals(provider, "StripeSandbox", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
                {
                    throw new InvalidOperationException("Stripe secret key is not configured.");
                }

                if (string.IsNullOrWhiteSpace(workshop.StripeConnectedAccountId))
                {
                    throw new ArgumentException("Workshop Stripe account is not connected.");
                }

                if (!workshop.StripePayoutsEnabled)
                {
                    throw new ArgumentException("Workshop Stripe account is connected but not payout-enabled.");
                }

                var transferService = new TransferService();
                var transfer = await transferService.CreateAsync(new TransferCreateOptions
                {
                    Amount = ConvertAmountToStripeMinorUnit(estimate.TotalAmount),
                    Currency = "myr",
                    Destination = workshop.StripeConnectedAccountId.Trim(),
                    Description = $"Workshop payment for claim {estimate.ClaimId}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["EstimateId"] = estimate.EstimateId.ToString(),
                        ["ClaimId"] = estimate.ClaimId.ToString(),
                        ["WorkshopId"] = estimate.WorkshopId.ToString(),
                        ["ApprovalSource"] = estimate.IsStpApproved ? "STP" : "ManualReview"
                    }
                });

                _logger.LogInformation(
                    "Created Stripe sandbox transfer {TransferId} for workshop {WorkshopId} and estimate {EstimateId}.",
                    transfer.Id,
                    estimate.WorkshopId,
                    estimate.EstimateId);

                return new WorkshopPaymentProviderResolution
                {
                    Provider = "StripeSandbox",
                    ProviderReference = transfer.Id,
                    Status = "Paid"
                };
            }

            return await Task.FromResult(new WorkshopPaymentProviderResolution
            {
                Provider = "MockSandbox",
                ProviderReference = BuildMockReference(),
                Status = "Paid"
            });
        }

        private static string BuildMockReference()
        {
            return $"mock_payout_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..45];
        }

        private static long ConvertAmountToStripeMinorUnit(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Transfer amount must be greater than zero.");
            }

            return decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
        }
    }
}
