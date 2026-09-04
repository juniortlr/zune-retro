using System.Buffers.Binary;
using System.Text.Json;
using EmberStart.Core.Activation;

namespace EmberStart.Windows.Instance;

internal static class ActivationPipeProtocol
{
    public const int MaximumMessageBytes = 4 * 1024;
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNameCaseInsensitive = false,
    };

    public static Task WriteRequestAsync(Stream stream, ActivationRequest request, CancellationToken cancellationToken) =>
        WriteAsync(stream, request, cancellationToken);

    public static Task WriteResponseAsync(Stream stream, ActivationResponse response, CancellationToken cancellationToken) =>
        WriteAsync(stream, response, cancellationToken);

    public static Task<ActivationRequest> ReadRequestAsync(Stream stream, CancellationToken cancellationToken) =>
        ReadAsync<ActivationRequest>(stream, cancellationToken);

    public static Task<ActivationResponse> ReadResponseAsync(Stream stream, CancellationToken cancellationToken) =>
        ReadAsync<ActivationResponse>(stream, cancellationToken);

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

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions)
            ?? throw new InvalidDataException("Activation payload could not be decoded.");
    }

    private static CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(OperationTimeout);
        return source;
    }
}
