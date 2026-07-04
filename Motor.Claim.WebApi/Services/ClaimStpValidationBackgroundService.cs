using Motor.Claim.Application.Services;

namespace Motor.Claim.WebApi.Services
{
    public class ClaimStpValidationBackgroundService : BackgroundService
    {
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
                Guid claimId;
                try
                {
                    claimId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

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
}
