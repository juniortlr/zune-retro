namespace EmberStart.Windows.Instance;

internal sealed record PipePeerIdentity(uint ProcessId, long CreationTime, string UserSid, int SessionId, string ImagePath);
