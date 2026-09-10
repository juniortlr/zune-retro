using EmberStart.Windows.Security;

namespace EmberStart.Windows.IntegrationTests.Instance;

internal sealed class StandardUserFactAttribute : FactAttribute
{
    public StandardUserFactAttribute()
    {
        if (!ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident &&
            Environment.GetEnvironmentVariable("EMBERSTART_REQUIRE_IPC_TESTS") != "1")
        {
            Skip = "Requires a standard-user Windows test host; elevated CI is not runtime IPC evidence. " +
                "Set EMBERSTART_REQUIRE_IPC_TESTS=1 in the qualification environment to require execution.";
        }
    }
}
