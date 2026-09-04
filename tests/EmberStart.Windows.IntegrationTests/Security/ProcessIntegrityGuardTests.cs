using EmberStart.Windows.Security;

namespace EmberStart.Windows.IntegrationTests.Security;

public sealed class ProcessIntegrityGuardTests
{
    [Fact]
    public void EvaluateCurrentProcess_ReturnsKnownIntegrity()
    {
        var result = ProcessIntegrityGuard.EvaluateCurrentProcess();

        Assert.NotEqual(ProcessIntegrityLevel.Unknown, result.Level);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}
