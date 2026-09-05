using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmberStart.Core.Activation;
using EmberStart.Core.Geometry;

namespace EmberStart.Windows.Instance;

internal static class ActivationPipeProtocol
{
    public const int MaximumMessageBytes = 4 * 1024;
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        MaxDepth = 8,
    };

    public static Task WriteRequestAsync(Stream stream, ActivationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var wire = new RequestWire
        {
            ProtocolVersion = request.ProtocolVersion,
            RequestId = request.RequestId,
            Command = request.Command,
            Source = request.Source,
            Anchor = request.Anchor is { } anchor
                ? new AnchorWire { Left = anchor.Left, Top = anchor.Top, Right = anchor.Right, Bottom = anchor.Bottom }
                : null,
            Edge = request.Edge,
        };
        _ = ValidateRequest(wire);
        return WriteAsync(stream, wire, cancellationToken);
    }

    public static Task WriteResponseAsync(Stream stream, ActivationResponse response, CancellationToken cancellationToken) =>
        WriteAsync(stream, new ResponseWire
        {
            RequestId = response.RequestId,
            Accepted = response.Accepted,
            Code = response.Code,
        }, cancellationToken);

    public static async Task<ActivationRequest> ReadRequestAsync(Stream stream, CancellationToken cancellationToken) =>
        ValidateRequest(await ReadAsync<RequestWire>(stream, cancellationToken).ConfigureAwait(false));

    public static async Task<ActivationResponse> ReadResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var wire = await ReadAsync<ResponseWire>(stream, cancellationToken).ConfigureAwait(false);
        if (wire.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(wire.Code) || wire.Code.Length > 64)
        {
            throw new InvalidDataException("Activation response is invalid.");
        }

        return new ActivationResponse(wire.RequestId, wire.Accepted, wire.Code);
    }

    private static ActivationRequest ValidateRequest(RequestWire wire)
    {
        if (wire.ProtocolVersion != ActivationRequest.CurrentProtocolVersion ||
            wire.RequestId == Guid.Empty ||
            !Enum.IsDefined(wire.Command) || !Enum.IsDefined(wire.Source) ||
            (wire.Edge is { } edge && !Enum.IsDefined(edge)))
        {
            throw new InvalidDataException("Activation version, identity, or command is invalid.");
        }

        if ((wire.Anchor is null) != (wire.Edge is null) ||
            (wire.Anchor is not null &&
                (wire.Source != ActivationSource.RetroBar || wire.Command != ActivationCommand.Toggle)))
        {
            throw new InvalidDataException("Activation placement context is invalid.");
        }

        PhysicalRect? anchor = wire.Anchor is { } value
            ? new PhysicalRect(value.Left, value.Top, value.Right, value.Bottom)
            : null;

        // Signed coordinates are valid. Bad geometry falls back; topology is checked by the resident.
        // Capping dimensions at Int32.MaxValue also keeps area multiplication within Int64.
        if (anchor is { } bounds &&
            (!bounds.IsOrdered || bounds.Width > int.MaxValue || bounds.Height > int.MaxValue))
        {
            anchor = null;
        }

        return new ActivationRequest(wire.ProtocolVersion, wire.RequestId, wire.Command, wire.Source,
            anchor, anchor is null ? null : wire.Edge);
    }

    private static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        if (payload.Length > MaximumMessageBytes)
        {
            throw new InvalidDataException("Activation message exceeds the 4 KiB protocol limit.");
        }

        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        using var timeout = CreateTimeout(cancellationToken);
        await stream.WriteAsync(header, timeout.Token).ConfigureAwait(false);
        await stream.WriteAsync(payload, timeout.Token).ConfigureAwait(false);
        await stream.FlushAsync(timeout.Token).ConfigureAwait(false);
    }

    private static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[sizeof(int)];
        using var timeout = CreateTimeout(cancellationToken);
        await stream.ReadExactlyAsync(header, timeout.Token).ConfigureAwait(false);

        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (payloadLength is <= 0 or > MaximumMessageBytes)
        {
            throw new InvalidDataException("Activation frame length is outside protocol bounds.");
        }

        var payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload, timeout.Token).ConfigureAwait(false);

        try
        {
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions)
                ?? throw new InvalidDataException("Activation payload could not be decoded.");
        }
        catch (JsonException)
        {
            // Do not retain the exception: its path/message can include untrusted payload text.
            throw new InvalidDataException("Activation payload does not match the wire schema.");
        }
    }

    private static CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(OperationTimeout);
        return source;
    }

    private sealed class RequestWire
    {
        public required int ProtocolVersion { get; init; }
        public required Guid RequestId { get; init; }
        public required ActivationCommand Command { get; init; }
        public required ActivationSource Source { get; init; }
        public required AnchorWire? Anchor { get; init; }
        public required TaskbarEdge? Edge { get; init; }
    }

    private sealed class AnchorWire
    {
        public required int Left { get; init; }
        public required int Top { get; init; }
        public required int Right { get; init; }
        public required int Bottom { get; init; }
    }

    private sealed class ResponseWire
    {
        public required Guid RequestId { get; init; }
        public required bool Accepted { get; init; }
        public required string? Code { get; init; }
    }
}
