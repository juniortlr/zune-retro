namespace EmberStart.Windows.Instance;

/// <summary>A failed activation send attempt with the stage it failed at.</summary>
public sealed class ActivationSendException : Exception
{
    public ActivationSendException(ActivationSendStage stage, Exception innerException)
        : base($"Activation send failed at stage {stage}.", innerException)
    {
        Stage = stage;
    }

    /// <summary>The last stage the attempt reached before failing.</summary>
    public ActivationSendStage Stage { get; }

    /// <summary>
    /// True when the request frame may have reached the activation handler. A caller must never
    /// automatically repeat an operation whose transmission may have begun; a read/ACK failure
    /// does not prove the handler did not apply a toggle.
    /// </summary>
    public bool TransmissionStarted => Stage is ActivationSendStage.RequestWritten or ActivationSendStage.ResponseRead;
}
