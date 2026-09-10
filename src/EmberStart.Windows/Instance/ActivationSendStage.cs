namespace EmberStart.Windows.Instance;

/// <summary>Progression of a secondary's single send attempt. Transmission begins at <see cref="RequestWritten"/>.</summary>
public enum ActivationSendStage
{
    /// <summary>The pipe connection has not been established yet. No bytes reached another process.</summary>
    Connecting = 0,

    /// <summary>The connection was established but the peer has not been validated. No request bytes were written.</summary>
    Connected = 1,

    /// <summary>The server was validated but no request bytes were written yet.</summary>
    Validated = 2,

    /// <summary>The full request frame was handed to the transport. The handler may have applied it.</summary>
    RequestWritten = 3,

    /// <summary>A complete response frame was read. The exchange is finished.</summary>
    ResponseRead = 4,
}
