using System.Threading.Channels;

namespace Reklio.Api.BackgroundJobs;

public interface IClaimQueue
{
    ValueTask EnqueueAsync(int claimId, CancellationToken cancellationToken = default);

    ChannelReader<int> Reader { get; }
}