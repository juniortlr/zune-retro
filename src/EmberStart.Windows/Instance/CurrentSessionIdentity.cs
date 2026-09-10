using System.Diagnostics;
using System.Security.Principal;
using EmberStart.Core.Activation;
using EmberStart.Core.Instance;

namespace EmberStart.Windows.Instance;

public sealed record CurrentSessionIdentity(
    string UserSid,
    int SessionId,
    InstanceIdentity Names)
{
    public static CurrentSessionIdentity Create()
    {
        using var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
        var sid = identity.User?.Value ?? throw new InvalidOperationException("The current Windows SID is unavailable.");
        var sessionId = Process.GetCurrentProcess().SessionId;

        return new CurrentSessionIdentity(
            sid,
            sessionId,
            InstanceIdentity.Create(sid, sessionId, ActivationRequest.CurrentProtocolVersion));
    }
}
