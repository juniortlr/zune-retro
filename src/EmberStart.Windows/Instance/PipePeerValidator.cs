using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using EmberStart.Windows.Security;
using Windows.Win32;

namespace EmberStart.Windows.Instance;

internal static class PipePeerValidator
{
    public static bool IsClientAllowed(
        NamedPipeServerStream pipe,
        string expectedSid,
        int expectedSessionId)
    {
        if (!PInvoke.GetNamedPipeClientProcessId(
                pipe.SafePipeHandle,
                out var processId) ||
            !IsProcessInSession(processId, expectedSessionId))
        {
            return false;
        }

        var allowed = false;
        pipe.RunAsClient(() =>
        {
            using var identity = WindowsIdentity.GetCurrent(ifImpersonating: true);
            if (identity?.User?.Value != expectedSid)
            {
                return;
            }

            var integrity = ProcessIntegrityGuard.Evaluate(identity.AccessToken);
            allowed = integrity.MayBecomeResident;
        });

        return allowed;
    }

    public static bool IsServerInSession(NamedPipeClientStream pipe, int expectedSessionId) =>
        PInvoke.GetNamedPipeServerProcessId(
            pipe.SafePipeHandle,
            out var processId) &&
        IsProcessInSession(processId, expectedSessionId);

    private static bool IsProcessInSession(uint processId, int expectedSessionId)
    {
        try
        {
            using var process = Process.GetProcessById(checked((int)processId));
            return process.SessionId == expectedSessionId;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
