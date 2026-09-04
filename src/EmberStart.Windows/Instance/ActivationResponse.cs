namespace EmberStart.Windows.Instance;

public sealed record ActivationResponse(Guid RequestId, bool Accepted, string Code);
