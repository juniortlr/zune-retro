using EmberStart.Core.Instance;

namespace EmberStart.Windows.Instance;

internal sealed class ActivationAdmissionLimiter(TimeProvider clock)
{
    private const int MaximumPeers = 128;
    private readonly object _gate = new();
    private readonly long _started = clock.GetTimestamp();
    private readonly Dictionary<(uint Pid, long Created), (TokenBucket Bucket, TimeSpan Seen)> _peers = [];
    private readonly TokenBucket _global = new(80, 160, TimeSpan.Zero);

    public bool TryAdmit(PipePeerIdentity peer)
    {
        lock (_gate)
        {
            var now = clock.GetElapsedTime(_started);
            if (!_global.TryTake(now))
            {
                return false;
            }

            var key = (peer.ProcessId, peer.CreationTime);
            if (!_peers.TryGetValue(key, out var state))
            {
                // Do not evict live quotas on churn. Only expire fully-refilled idle entries.
                foreach (var expired in _peers.Where(pair => now - pair.Value.Seen >= TimeSpan.FromSeconds(60))
                    .Select(pair => pair.Key).ToArray())
                {
                    _peers.Remove(expired);
                }

                if (_peers.Count == MaximumPeers)
                {
                    return false;
                }

                state = (new TokenBucket(20, 40, now), now);
            }

            _peers[key] = (state.Bucket, now);
            return state.Bucket.TryTake(now);
        }
    }
}
