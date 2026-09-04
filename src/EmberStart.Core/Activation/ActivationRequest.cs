using EmberStart.Core.Geometry;

namespace EmberStart.Core.Activation;

public sealed record ActivationRequest(
    int ProtocolVersion,
    Guid RequestId,
    ActivationCommand Command,
    ActivationSource Source,
    PhysicalRect? Anchor,
    TaskbarEdge? Edge)
{
    public const int CurrentProtocolVersion = 1;

    public static ActivationRequest CreateSimple(ActivationCommand command, ActivationSource source) =>
        new(CurrentProtocolVersion, Guid.NewGuid(), command, source, null, null);
}
