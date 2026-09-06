using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationAdmissionLimiterTests
{
    [Fact]
    public void PeerQuota_IsKeyedByProcessIdAndCreationTime()
    {
        var clock = new ManualTimeProvider();
        var limiter = new ActivationAdmissionLimiter(clock);
        var original = Peer(42, 100);

        Admit(limiter, original, 40);
        Assert.False(limiter.TryAdmit(original));
        Assert.True(limiter.TryAdmit(Peer(42, 101)));
        Assert.True(limiter.TryAdmit(Peer(43, 100)));
    }

    [Fact]
    public void PeerQuota_RefillsFractionallyAfterDeniedPolls()
    {
        var clock = new ManualTimeProvider();
        var limiter = new ActivationAdmissionLimiter(clock);
        var peer = Peer(1, 1);
        Admit(limiter, peer, 40);

        for (var milliseconds = 10; milliseconds < 50; milliseconds += 10)
        {
            clock.Advance(TimeSpan.FromMilliseconds(10));
            Assert.False(limiter.TryAdmit(peer));
        }

        clock.Advance(TimeSpan.FromMilliseconds(10));
        Assert.True(limiter.TryAdmit(peer));
    }

    [Fact]
    public void GlobalQuota_AllowsBurstOfOneHundredSixtyThenRefillsAtEightyPerSecond()
    {
        var clock = new ManualTimeProvider();
        var limiter = new ActivationAdmissionLimiter(clock);
        for (uint processId = 1; processId <= 4; processId++)
        {
            Admit(limiter, Peer(processId, 1), 40);
        }

        var freshPeer = Peer(5, 1);
        Assert.False(limiter.TryAdmit(freshPeer));
        clock.Advance(TimeSpan.FromMilliseconds(6.25));
        Assert.False(limiter.TryAdmit(freshPeer));
        clock.Advance(TimeSpan.FromMilliseconds(6.25));
        Assert.True(limiter.TryAdmit(freshPeer));
    }

    [Fact]
    public void PeerTableAtCapacity_DoesNotEvictLiveEntries()
    {
        var clock = new ManualTimeProvider();
        var limiter = new ActivationAdmissionLimiter(clock);
        for (uint processId = 1; processId <= 128; processId++)
        {
            Assert.True(limiter.TryAdmit(Peer(processId, 1)));
        }

        Assert.False(limiter.TryAdmit(Peer(129, 1)));
        Assert.True(limiter.TryAdmit(Peer(1, 1)));
    }

    [Fact]
    public void IdlePeer_ExpiresAfterSixtySecondsAndMakesRoomForNewPeer()
    {
        var clock = new ManualTimeProvider();
        var limiter = new ActivationAdmissionLimiter(clock);
        for (uint processId = 1; processId <= 128; processId++)
        {
            Assert.True(limiter.TryAdmit(Peer(processId, 1)));
        }

        clock.Advance(TimeSpan.FromSeconds(59.999));
        Assert.False(limiter.TryAdmit(Peer(129, 1)));
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(limiter.TryAdmit(Peer(129, 1)));
    }

    private static PipePeerIdentity Peer(uint processId, long creationTime) =>
        new(processId, creationTime, "S-1-5-21-test", 1, "C:\\Test\\peer.exe");

    private static void Admit(ActivationAdmissionLimiter limiter, PipePeerIdentity peer, int count)
    {
        for (var index = 0; index < count; index++)
        {
            Assert.True(limiter.TryAdmit(peer));
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan duration) => _timestamp += duration.Ticks;
    }
}
