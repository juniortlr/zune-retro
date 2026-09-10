using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;
using Microsoft.Win32.SafeHandles;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class NamedObjectSecurityTests
{
    [StandardUserFact]
    public async Task ConnectedFirstPipe_CoexistsWithWaitingReplacement()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        await using var first = NamedObjectSecurity.CreatePipe(identity, instances: 3, first: true);
        await using var firstClient = CreateClient(identity);

        var firstWait = first.WaitForConnectionAsync();
        await ConnectAsync(firstClient);
        await firstWait.WaitAsync(TimeSpan.FromSeconds(2));

        await using var replacement = NamedObjectSecurity.CreatePipe(identity, instances: 3, first: false);
        var replacementWait = replacement.WaitForConnectionAsync();
        await firstClient.DisposeAsync();
        await first.DisposeAsync();

        var collision = Assert.Throws<UnauthorizedAccessException>(() =>
            NamedObjectSecurity.CreatePipe(identity, instances: 3, first: true));
        Assert.Equal(unchecked((int)0x80070005), collision.HResult);

        await using var replacementClient = CreateClient(identity);
        await ConnectAsync(replacementClient);
        await replacementWait.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(replacement.IsConnected);
    }

    [StandardUserFact]
    public void FirstPipeInstance_CollidesWhileNameIsOwned()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        using var owner = NamedObjectSecurity.CreatePipe(identity, instances: 3, first: true);

        var collision = Assert.Throws<UnauthorizedAccessException>(() =>
            NamedObjectSecurity.CreatePipe(identity, instances: 3, first: true));
        Assert.Equal(unchecked((int)0x80070005), collision.HResult);
    }

    [StandardUserFact]
    public void PipeDescriptorAndHandle_AreObservedAsOwnerOnlyProtectedAndNonInheritable()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        using var pipe = NamedObjectSecurity.CreatePipe(identity, instances: 3, first: true);
        var expectedSid = new SecurityIdentifier(identity.UserSid);
        var descriptor = new RawSecurityDescriptor(
            pipe.GetAccessControl().GetSecurityDescriptorBinaryForm(), 0);

        Assert.Equal(expectedSid, descriptor.Owner);
        Assert.True(descriptor.ControlFlags.HasFlag(ControlFlags.DiscretionaryAclProtected));
        var acl = Assert.IsType<RawAcl>(descriptor.DiscretionaryAcl);
        var ace = Assert.IsType<CommonAce>(Assert.Single(acl.Cast<GenericAce>()));
        Assert.Equal(AceQualifier.AccessAllowed, ace.AceQualifier);
        Assert.Equal(expectedSid, ace.SecurityIdentifier);
        Assert.Equal(AceFlags.None, ace.AceFlags);
        Assert.Equal((int)PipeAccessRights.FullControl, ace.AccessMask);
        Assert.False(ace.IsCallback);

        Assert.True(GetHandleInformation(pipe.SafePipeHandle, out var flags));
        Assert.False(flags.HasFlag(HandleFlags.Inherit));
    }

    private static CurrentSessionIdentity CreateIdentity()
    {
        var current = CurrentSessionIdentity.Create();
        var suffix = Guid.NewGuid().ToString("N");
        return current with
        {
            Names = new InstanceIdentity(
                $"Local\\EmberStart.Tests.{suffix}", $"EmberStart.Tests.{suffix}")
        };
    }

    private static NamedPipeClientStream CreateClient(CurrentSessionIdentity identity) => new(
        ".", identity.Names.PipeName, PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        TokenImpersonationLevel.Impersonation);

    private static Task ConnectAsync(NamedPipeClientStream client) =>
        client.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(2));

    private static void EnsureSuitableHost() => Assert.True(
        ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident,
        "Named-object security tests require a current-user medium-integrity Windows host.");

#pragma warning disable SYSLIB1054 // Test-only declaration avoids enabling unsafe generated interop.
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetHandleInformation(SafePipeHandle handle, out HandleFlags flags);
#pragma warning restore SYSLIB1054

    [Flags]
    private enum HandleFlags : uint
    {
        None = 0,
        Inherit = 1,
        ProtectFromClose = 2
    }
}
