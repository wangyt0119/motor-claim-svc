using Motor.Claim.Application.Services;
using Motor.Claim.Application.Interfaces;

namespace Motor.Claim.WebApi.Services
{
    public class ClaimStpValidationBackgroundService : BackgroundService
    {
        private static readonly TimeSpan QueueWaitTimeout = TimeSpan.FromSeconds(10);
        private const int PendingScanBatchSize = 5;

        private readonly IClaimStpValidationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ClaimStpValidationBackgroundService> _logger;

        public ClaimStpValidationBackgroundService(
            IClaimStpValidationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ClaimStpValidationBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid? queuedClaimId;
                try
                {
                    queuedClaimId = await _queue.DequeueAsync(QueueWaitTimeout, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (queuedClaimId.HasValue)
                {
                    await ProcessClaimAsync(queuedClaimId.Value);
                    continue;
                }

                await ProcessPendingClaimsAsync();
            }
        }

        private async Task ProcessPendingClaimsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();
                var claimIds = await claimRepository.GetPendingStpValidationClaimIdsAsync(PendingScanBatchSize);

                foreach (var claimId in claimIds)
                {
                    await ProcessClaimAsync(claimId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scan pending STP validation claims.");
            }
        }

        private async Task ProcessClaimAsync(Guid claimId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var claimService = scope.ServiceProvider.GetRequiredService<ClaimService>();
                await claimService.ProcessStpValidationAsync(claimId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process STP validation for claim {ClaimId}.", claimId);
            }
        }
    }
}
