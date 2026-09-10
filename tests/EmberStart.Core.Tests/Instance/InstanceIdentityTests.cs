using EmberStart.Core.Instance;

namespace EmberStart.Core.Tests.Instance;

public sealed class InstanceIdentityTests
{
    [Fact]
    public void Create_HashesSidAndScopesSessionAndProtocol()
    {
        const string sid = "S-1-5-21-123-456-789-1001";

        var first = InstanceIdentity.Create(sid, 2, 1);
        var same = InstanceIdentity.Create(sid, 2, 1);
        var otherSession = InstanceIdentity.Create(sid, 3, 1);

        Assert.Equal(first, same);
        Assert.NotEqual(first, otherSession);
        Assert.DoesNotContain(sid, first.MutexName, StringComparison.Ordinal);
        Assert.Contains(".2.v1", first.PipeName, StringComparison.Ordinal);
    }
}
