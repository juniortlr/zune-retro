using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace EmberStart.Windows.Instance;

internal static partial class NamedObjectSecurity
{
    public static Mutex CreateMutex(CurrentSessionIdentity identity, out bool createdNew)
    {
        var sid = new SecurityIdentifier(identity.UserSid);
        var security = new MutexSecurity();
        security.SetOwner(sid);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        // MutexAcl.Create opens existing objects with full-control rights. Trust is per user,
        // so this grants those rights only to that user, never a broader group.
        security.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
        var mutex = MutexAcl.Create(false, identity.Names.MutexName, out createdNew, security);
        try
        {
            Validate(mutex.GetAccessControl(), sid, (int)MutexRights.FullControl);
            ValidateHandle(mutex.SafeWaitHandle);
            return mutex;
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
    }

    public static NamedPipeServerStream CreatePipe(CurrentSessionIdentity identity, int instances, bool first)
    {
        var sid = new SecurityIdentifier(identity.UserSid);
        var security = new PipeSecurity();
        security.SetOwner(sid);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.FullControl, AccessControlType.Allow));
        // CurrentUserOnly would replace this descriptor. Keep explicit security on the server,
        // plus token validation; the client retains CurrentUserOnly as an extra owner check.
        var options = PipeOptions.Asynchronous | (first ? PipeOptions.FirstPipeInstance : PipeOptions.None);
        var pipe = NamedPipeServerStreamAcl.Create(identity.Names.PipeName, PipeDirection.InOut,
            instances, PipeTransmissionMode.Byte, options,
            ActivationPipeProtocol.MaximumMessageBytes, ActivationPipeProtocol.MaximumMessageBytes,
            security, HandleInheritability.None, (PipeAccessRights)0);
        try
        {
            Validate(pipe.GetAccessControl(), sid, (int)PipeAccessRights.FullControl);
            ValidateHandle(pipe.SafePipeHandle);
            return pipe;
        }
        catch
        {
            pipe.Dispose();
            throw;
        }
    }

    private static void Validate(ObjectSecurity security, SecurityIdentifier sid, int rights)
    {
        var descriptor = new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);
        var acl = descriptor.DiscretionaryAcl;
        if (descriptor.Owner != sid ||
            (descriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) == 0 ||
            acl is null || acl.Count != 1 || acl[0] is not CommonAce ace ||
            ace.AceQualifier != AceQualifier.AccessAllowed || ace.SecurityIdentifier != sid ||
            ace.AceFlags != AceFlags.None || ace.AccessMask != rights || ace.IsCallback)
        {
            throw new UnauthorizedAccessException("The named object's security descriptor is not trusted.");
        }
    }

    private static void ValidateHandle(SafeHandle handle)
    {
        if (!GetHandleInformation(handle, out var flags) || (flags & 1) != 0)
        {
            throw new UnauthorizedAccessException("The named object's handle is inheritable or unavailable.");
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetHandleInformation(SafeHandle handle, out uint flags);
}
