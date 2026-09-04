namespace EmberStart.Windows.Security;

public sealed record IntegrityDecision(
    bool MayBecomeResident,
    ProcessIntegrityLevel Level,
    string Message);
