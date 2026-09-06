namespace EmberStart.Core.Instance;

/// <summary>A bounded token bucket driven by a caller-supplied monotonic clock.</summary>
public sealed class TokenBucket
{
    private readonly int _ratePerSecond;
    private readonly int _capacity;
    private decimal _tokens;
    private TimeSpan _lastRefill;

    public TokenBucket(int ratePerSecond, int capacity, TimeSpan now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ratePerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(now, TimeSpan.Zero);
        _ratePerSecond = ratePerSecond;
        _capacity = capacity;
        _tokens = capacity;
        _lastRefill = now;
    }

    // Synchronization belongs to the caller; backwards time cannot mint tokens.
    public bool TryTake(TimeSpan now)
    {
        if (now < _lastRefill)
        {
            return false;
        }

        var refill = (decimal)(now - _lastRefill).Ticks * _ratePerSecond / TimeSpan.TicksPerSecond;
        _tokens = Math.Min(_capacity, _tokens + refill);
        _lastRefill = now;
        if (_tokens < 1)
        {
            return false;
        }

        _tokens--;
        return true;
    }
}
