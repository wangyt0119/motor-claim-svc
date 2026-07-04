using System.Threading.Channels;

namespace Motor.Claim.WebApi.Services
{
    public interface IClaimStpValidationQueue
    {
        ValueTask QueueAsync(Guid claimId);
        ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
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

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
