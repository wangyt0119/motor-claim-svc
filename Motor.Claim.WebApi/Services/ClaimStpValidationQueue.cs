using System.Threading.Channels;

namespace Motor.Claim.WebApi.Services
{
    public interface IClaimStpValidationQueue
    {
        ValueTask QueueAsync(Guid claimId);
        ValueTask<Guid?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }

    public class ClaimStpValidationQueue : IClaimStpValidationQueue
    {
        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        public ValueTask QueueAsync(Guid claimId)
        {
            return _queue.Writer.WriteAsync(claimId);
        }

        public async ValueTask<Guid?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                return await _queue.Reader.ReadAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }
    }
}
