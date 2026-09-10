using System.Security.Cryptography;
using System.Text;

namespace EmberStart.Core.Instance;

public sealed record InstanceIdentity(string MutexName, string PipeName)
{
    public static InstanceIdentity Create(string userSid, int sessionId, int protocolVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userSid);
        ArgumentOutOfRangeException.ThrowIfNegative(sessionId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(protocolVersion);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userSid));
        var sidIdentifier = Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        var suffix = $"{sidIdentifier}.{sessionId}.v{protocolVersion}";

        return new InstanceIdentity(
            $"Local\\EmberStart.{suffix}",
            $"EmberStart.{suffix}");
    }
}
