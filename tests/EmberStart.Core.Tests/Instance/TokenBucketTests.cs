using EmberStart.Core.Instance;

namespace EmberStart.Core.Tests.Instance;

public sealed class TokenBucketTests
{
    [Fact]
    public void InitialBurst_AllowsFortyThenRejects()
    {
        var bucket = new TokenBucket(20, 40, TimeSpan.Zero);

        Assert.All(Enumerable.Range(0, 40), _ => Assert.True(bucket.TryTake(TimeSpan.Zero)));
        Assert.False(bucket.TryTake(TimeSpan.Zero));
    }

    [Fact]
    public void FractionalRefill_AccumulatesAcrossFrequentDeniedPolling()
    {
        var bucket = ExhaustedBucket();

        Assert.False(bucket.TryTake(TimeSpan.FromMilliseconds(10)));
        Assert.False(bucket.TryTake(TimeSpan.FromMilliseconds(20)));
        Assert.False(bucket.TryTake(TimeSpan.FromMilliseconds(30)));
        Assert.False(bucket.TryTake(TimeSpan.FromMilliseconds(40)));
        Assert.True(bucket.TryTake(TimeSpan.FromMilliseconds(50)));
        Assert.False(bucket.TryTake(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void BackwardsTime_IsRejectedAndDoesNotMoveRefillBaseline()
    {
        var bucket = new TokenBucket(20, 1, TimeSpan.FromSeconds(10));
        Assert.True(bucket.TryTake(TimeSpan.FromSeconds(10)));

        Assert.False(bucket.TryTake(TimeSpan.FromSeconds(9)));
        Assert.True(bucket.TryTake(TimeSpan.FromMilliseconds(10_050)));
    }

    [Fact]
    public void LongIdle_RefillIsCappedAtCapacity()
    {
        var bucket = ExhaustedBucket();

        Assert.All(Enumerable.Range(0, 40), _ => Assert.True(bucket.TryTake(TimeSpan.FromDays(30))));
        Assert.False(bucket.TryTake(TimeSpan.FromDays(30)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_RejectsNonPositiveArguments(int rate, int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucket(rate, capacity, TimeSpan.Zero));

    [Fact]
    public void Constructor_RejectsNegativeInitialTime() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucket(1, 1, TimeSpan.FromTicks(-1)));

    private static TokenBucket ExhaustedBucket()
    {
        var bucket = new TokenBucket(20, 40, TimeSpan.Zero);
        for (var index = 0; index < 40; index++)
        {
            Assert.True(bucket.TryTake(TimeSpan.Zero));
        }

        return bucket;
    }
}
