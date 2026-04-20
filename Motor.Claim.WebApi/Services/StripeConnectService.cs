using Microsoft.Extensions.Options;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.WebApi.Configuration;
using Motor.Claim.WebApi.Models;
using Stripe;

namespace Motor.Claim.WebApi.Services
{
    public class StripeConnectService
    {
        private readonly IWorkshopRepository _workshopRepository;
        private readonly StripeOptions _stripeOptions;

        public StripeConnectService(
            IWorkshopRepository workshopRepository,
            IOptions<StripeOptions> stripeOptions)
        {
            _workshopRepository = workshopRepository;
            _stripeOptions = stripeOptions.Value;
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
        }

        public async Task<CreateStripeConnectedAccountResponse> CreateConnectedAccountAsync(Guid workshopId)
        {
            ValidateStripeConfiguration();

            var workshop = await GetWorkshopAsync(workshopId);

            if (!string.IsNullOrWhiteSpace(workshop.StripeConnectedAccountId))
            {
                var existing = await new AccountService().GetAsync(workshop.StripeConnectedAccountId);
                await SyncWorkshopStripeStateAsync(workshop, existing);
                return MapCreateAccountResponse(workshop);
            }

            var accountService = new AccountService();
            var account = await accountService.CreateAsync(new AccountCreateOptions
            {
                Type = "standard",
                Country = "MY",
                Email = GetPrimaryEmail(workshop),
                Metadata = new Dictionary<string, string>
                {
                    ["WorkshopId"] = workshop.WorkshopId.ToString(),
                    ["WorkshopName"] = workshop.Name
                }
            });

            workshop.StripeConnectedAccountId = account.Id;
            await SyncWorkshopStripeStateAsync(workshop, account);
            return MapCreateAccountResponse(workshop);
        }

        public async Task<CreateStripeOnboardingLinkResponse> CreateOnboardingLinkAsync(Guid workshopId, string refreshUrl, string returnUrl)
        {
            ValidateStripeConfiguration();

            var workshop = await GetWorkshopAsync(workshopId);

            if (string.IsNullOrWhiteSpace(workshop.StripeConnectedAccountId))
            {
                throw new ArgumentException("Stripe connected account has not been created for this workshop.");
            }

            var linkService = new AccountLinkService();
            var link = await linkService.CreateAsync(new AccountLinkCreateOptions
            {
                Account = workshop.StripeConnectedAccountId,
                RefreshUrl = string.IsNullOrWhiteSpace(_stripeOptions.RefreshUrl) ? refreshUrl : _stripeOptions.RefreshUrl,
                ReturnUrl = string.IsNullOrWhiteSpace(_stripeOptions.ReturnUrl) ? returnUrl : _stripeOptions.ReturnUrl,
                Type = "account_onboarding"
            });

            workshop.StripeLastSyncedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(workshop.StripeOnboardingStatus))
            {
                workshop.StripeOnboardingStatus = "OnboardingStarted";
            }

            await _workshopRepository.UpdateAsync(workshop);

            return new CreateStripeOnboardingLinkResponse
            {
                Url = link.Url,
                StripeLastSyncedAt = workshop.StripeLastSyncedAt
            };
        }

        public async Task<StripeWorkshopStatusResponse> GetWorkshopStripeStatusAsync(Guid workshopId)
        {
            ValidateStripeConfiguration();

            var workshop = await GetWorkshopAsync(workshopId);

            if (!string.IsNullOrWhiteSpace(workshop.StripeConnectedAccountId))
            {
                var account = await new AccountService().GetAsync(workshop.StripeConnectedAccountId);
                await SyncWorkshopStripeStateAsync(workshop, account);
            }

            return MapStatus(workshop);
        }

        public async Task HandleAccountUpdatedAsync(Account account)
        {
            var workshopId = TryGetWorkshopId(account);
            if (!workshopId.HasValue)
            {
                var matched = await _workshopRepository.FindAsync(x => x.StripeConnectedAccountId == account.Id);
                var workshop = matched.FirstOrDefault();
                if (workshop == null)
                {
                    return;
                }

                await SyncWorkshopStripeStateAsync(workshop, account);
                return;
            }

            var targetWorkshop = await _workshopRepository.GetByIdAsync(workshopId.Value);
            if (targetWorkshop == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetWorkshop.StripeConnectedAccountId))
            {
                targetWorkshop.StripeConnectedAccountId = account.Id;
            }

            await SyncWorkshopStripeStateAsync(targetWorkshop, account);
        }

        private async Task<WorkshopEntity> GetWorkshopAsync(Guid workshopId)
        {
            var workshop = await _workshopRepository.GetByIdAsync(workshopId);
            if (workshop == null)
            {
                throw new ArgumentException("Workshop not found.");
            }

            if (!workshop.IsPanelWorkshop)
            {
                throw new ArgumentException("Stripe Connect is only available for panel workshops.");
            }

            return workshop;
        }

        private async Task SyncWorkshopStripeStateAsync(WorkshopEntity workshop, Account account)
        {
            workshop.StripeConnectedAccountId = account.Id;
            workshop.StripeChargesEnabled = account.ChargesEnabled;
            workshop.StripePayoutsEnabled = account.PayoutsEnabled;
            workshop.StripeOnboardingStatus = ResolveOnboardingStatus(account);
            workshop.StripeLastSyncedAt = DateTime.UtcNow;
            await _workshopRepository.UpdateAsync(workshop);
        }

        private static string ResolveOnboardingStatus(Account account)
        {
            if (account.PayoutsEnabled)
            {
                return "Completed";
            }

            if (account.DetailsSubmitted && !account.PayoutsEnabled)
            {
                return "PendingReview";
            }

            return "OnboardingRequired";
        }

        private static Guid? TryGetWorkshopId(Account account)
        {
            if (account.Metadata != null
                && account.Metadata.TryGetValue("WorkshopId", out var workshopIdValue)
                && Guid.TryParse(workshopIdValue, out var workshopId))
            {
                return workshopId;
            }

            return null;
        }

        private static string? GetPrimaryEmail(WorkshopEntity workshop)
        {
            if (string.IsNullOrWhiteSpace(workshop.Email))
            {
                return null;
            }

            try
            {
                var emails = System.Text.Json.JsonSerializer.Deserialize<List<string>>(workshop.Email);
                return emails?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
            }
            catch
            {
                return workshop.Email.Trim();
            }
        }

        private CreateStripeConnectedAccountResponse MapCreateAccountResponse(WorkshopEntity workshop)
        {
            return new CreateStripeConnectedAccountResponse
            {
                StripeConnectedAccountId = workshop.StripeConnectedAccountId ?? string.Empty,
                StripeOnboardingStatus = workshop.StripeOnboardingStatus ?? "OnboardingRequired",
                StripeChargesEnabled = workshop.StripeChargesEnabled,
                StripePayoutsEnabled = workshop.StripePayoutsEnabled,
                StripeLastSyncedAt = workshop.StripeLastSyncedAt
            };
        }

        private static StripeWorkshopStatusResponse MapStatus(WorkshopEntity workshop)
        {
            return new StripeWorkshopStatusResponse
            {
                WorkshopId = workshop.WorkshopId,
                WorkshopName = workshop.Name,
                StripeConnectedAccountId = workshop.StripeConnectedAccountId,
                StripeOnboardingStatus = workshop.StripeOnboardingStatus,
                StripeChargesEnabled = workshop.StripeChargesEnabled,
                StripePayoutsEnabled = workshop.StripePayoutsEnabled,
                StripeLastSyncedAt = workshop.StripeLastSyncedAt
            };
        }

        private void ValidateStripeConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
            {
                throw new InvalidOperationException("Stripe secret key is not configured.");
            }
        }
    }
}
