using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace EmberStart.Windows.Security;

public static partial class ProcessIntegrityGuard
{
    private const int TokenIntegrityLevel = 25;
    private const int SecurityMandatoryLowRid = 0x1000;
    private const int SecurityMandatoryMediumRid = 0x2000;
    private const int SecurityMandatoryHighRid = 0x3000;
    private const int SecurityMandatorySystemRid = 0x4000;
    private const int SecurityMandatoryProtectedProcessRid = 0x5000;

    public static IntegrityDecision EvaluateCurrentProcess()
    {
        using var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
        return Evaluate(identity.AccessToken);
    }

    internal static IntegrityDecision Evaluate(SafeAccessTokenHandle token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _ = GetTokenInformation(token, TokenIntegrityLevel, nint.Zero, 0, out var requiredLength);
        var error = Marshal.GetLastPInvokeError();
        if (requiredLength <= 0 && error != 122)
        {
            throw new Win32Exception(error, "Could not determine the token integrity buffer size.");
        }

        var buffer = Marshal.AllocHGlobal(requiredLength);
        try
        {
            if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, requiredLength, out _))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not read token integrity.");
            }

            var sid = Marshal.ReadIntPtr(buffer);
            var subAuthorityCount = Marshal.ReadByte(sid, 1);
            if (subAuthorityCount == 0)
            {
                return new IntegrityDecision(false, ProcessIntegrityLevel.Unknown, "Token has no integrity authority.");
            }

            var ridOffset = checked(8 + ((subAuthorityCount - 1) * sizeof(uint)));
            var rid = Marshal.ReadInt32(sid, ridOffset);
            var level = MapRid(rid);
            var allowed = level is ProcessIntegrityLevel.Untrusted or
                ProcessIntegrityLevel.Low or
                ProcessIntegrityLevel.Medium;

            return new IntegrityDecision(
                allowed,
                level,
                allowed
                    ? $"Integrity level {level} is allowed."
                    : "Ember Start will not remain resident above medium integrity. Launch it normally from Explorer.");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static ProcessIntegrityLevel MapRid(int rid) => rid switch
    {
        < SecurityMandatoryLowRid => ProcessIntegrityLevel.Untrusted,
        < SecurityMandatoryMediumRid => ProcessIntegrityLevel.Low,
        < SecurityMandatoryHighRid => ProcessIntegrityLevel.Medium,
        < SecurityMandatorySystemRid => ProcessIntegrityLevel.High,
        < SecurityMandatoryProtectedProcessRid => ProcessIntegrityLevel.System,
        _ => ProcessIntegrityLevel.Protected,
    };

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetTokenInformation(
        SafeAccessTokenHandle tokenHandle,
        int tokenInformationClass,
        nint tokenInformation,
        int tokenInformationLength,
        out int returnLength);
}
