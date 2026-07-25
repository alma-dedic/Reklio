using System.Threading.Channels;

namespace Reklio.Api.BackgroundJobs;

// In-memory red čekanja (T3.1). Volatilan — sadržaj se gubi na restartu;
// trajnost obezbjeđuje crash-recovery iz baze (ClaimProcessingWorker).
public class ClaimQueue : IClaimQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<int> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(int claimId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(claimId, cancellationToken);
    }
}