using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class CurrentSessionIdentityTests
{
    [Fact]
    public void Create_ScopesNamesWithoutExposingSid()
    {
        var identity = CurrentSessionIdentity.Create();

        Assert.False(string.IsNullOrWhiteSpace(identity.UserSid));
        Assert.True(identity.SessionId >= 0);
        Assert.DoesNotContain(identity.UserSid, identity.Names.MutexName, StringComparison.Ordinal);
        Assert.DoesNotContain(identity.UserSid, identity.Names.PipeName, StringComparison.Ordinal);
    }
}
