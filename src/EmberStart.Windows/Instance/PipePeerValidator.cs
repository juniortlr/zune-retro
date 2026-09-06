using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.IO.Pipes;
using EmberStart.Windows.Security;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace EmberStart.Windows.Instance;

internal static partial class PipePeerValidator
{
    public static bool TryValidateClient(NamedPipeServerStream pipe, CurrentSessionIdentity expected,
        out PipePeerIdentity? peer)
    {
        peer = null;
        if (!PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var pid) ||
            !TryCapture(pid, expected, out var captured))
        {
            return false;
        }

        var matches = false;
        try
        {
            pipe.RunAsClient(() =>
            {
                using var identity = WindowsIdentity.GetCurrent(ifImpersonating: true);
                matches = identity?.User?.Value == expected.UserSid &&
                    ProcessIntegrityGuard.Evaluate(identity.AccessToken).Level == ProcessIntegrityLevel.Medium;
            });

            if (!matches || !PInvoke.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var after) || after != pid)
            {
                return false;
            }

            peer = captured;
            return true;
        }
        catch (Exception exception) when (IsValidationFailure(exception))
        {
            return false;
        }
    }

    public static bool IsServerAllowed(NamedPipeClientStream pipe, CurrentSessionIdentity expected, string expectedImage) =>
        PInvoke.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var pid) &&
        TryCapture(pid, expected, out var peer) &&
        string.Equals(peer!.ImagePath, expectedImage, StringComparison.OrdinalIgnoreCase) &&
        PInvoke.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var after) && after == pid;

    private static unsafe bool TryCapture(uint pid, CurrentSessionIdentity expected, out PipePeerIdentity? peer)
    {
        peer = null;
        try
        {
            using var process = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
            if (process.IsInvalid || !GetProcessTimes(process, out var creation, out _, out _, out _) ||
                !OpenProcessToken(process, 0x0008, out var token)) // TOKEN_QUERY
            {
                return false;
            }

            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            {
                if (identity.User?.Value != expected.UserSid ||
                    ProcessIntegrityGuard.Evaluate(token).Level != ProcessIntegrityLevel.Medium ||
                    !GetTokenInformation(token, 12, out var sessionId, sizeof(int), out _) ||
                    sessionId != expected.SessionId)
                {
                    return false;
                }

                var image = new char[32768];
                uint length = (uint)image.Length;
                fixed (char* imagePointer = image)
                {
                    if (!QueryFullProcessImageName(process, 0, imagePointer, ref length) ||
                        !GetExitCodeProcess(process, out var exitCode) || exitCode != 259)
                    {
                        return false;
                    }
                }

                peer = new PipePeerIdentity(pid, creation, expected.UserSid, sessionId, new string(image, 0, (int)length));
                return true;
            }
        }
        catch (Exception exception) when (IsValidationFailure(exception))
        {
            return false;
        }
    }

    private static bool IsValidationFailure(Exception exception) =>
        exception is Win32Exception or UnauthorizedAccessException or ArgumentException or InvalidOperationException or IOException;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial SafeProcessHandle OpenProcess(uint access,
        [MarshalAs(UnmanagedType.Bool)] bool inherit, uint processId);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass,
        out int value, int length, out int returnedLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessTimes(SafeProcessHandle process,
        out long creation, out long exit, out long kernel, out long user);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool QueryFullProcessImageName(SafeProcessHandle process, uint flags,
        char* name, ref uint length);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetExitCodeProcess(SafeProcessHandle process, out uint exitCode);
}
