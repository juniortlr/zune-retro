using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using EmberStart.Core.Activation;
using EmberStart.Core.Geometry;
using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationPipeProtocolTests
{
    [Fact]
    public async Task Request_WritesExactlyTheSixCanonicalFields()
    {
        var request = new ActivationRequest(1, Guid.NewGuid(), ActivationCommand.Toggle,
            ActivationSource.RetroBar, new PhysicalRect(-20, -10, 300, 400), TaskbarEdge.Left);
        using var document = JsonDocument.Parse(await WriteAndExtractJsonAsync(request));
        Assert.Equal(["ProtocolVersion", "RequestId", "Command", "Source", "Anchor", "Edge"],
            document.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["Left", "Top", "Right", "Bottom"],
            document.RootElement.GetProperty("Anchor").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task NegativeOrderedCoordinates_RoundTrip()
    {
        var request = new ActivationRequest(1, Guid.NewGuid(), ActivationCommand.Toggle,
            ActivationSource.RetroBar, new PhysicalRect(-1920, -1080, -1, -1), TaskbarEdge.Top);
        Assert.Equal(request, await RoundTripAsync(request));
    }

    [Theory]
    [InlineData(10, 0, 10, 20)]
    [InlineData(20, 0, 10, 20)]
    [InlineData(int.MinValue, int.MinValue, int.MaxValue, int.MaxValue)]
    public async Task UnorderedOrOversizedAnchor_NormalizesToFallback(int left, int top, int right, int bottom)
    {
        var json = $"{{\"ProtocolVersion\":1,\"RequestId\":\"{Guid.NewGuid()}\"," +
            $"\"Command\":0,\"Source\":2,\"Anchor\":{{\"Left\":{left},\"Top\":{top}," +
            $"\"Right\":{right},\"Bottom\":{bottom}}},\"Edge\":3}}";
        var restored = await ReadPayloadAsync(Utf8(json));
        Assert.Null(restored.Anchor);
        Assert.Null(restored.Edge);
    }

    [Fact]
    public async Task FrameAtCap_IsAccepted_AndFrameOverCapIsRejected()
    {
        var json = await WriteAndExtractJsonAsync(ActivationRequest.CreateSimple(
            ActivationCommand.Toggle, ActivationSource.CommandLine));
        var restored = await ReadPayloadAsync(Encoding.UTF8.GetBytes(json.PadRight(ActivationPipeProtocol.MaximumMessageBytes)));
        Assert.Equal(1, restored.ProtocolVersion);
        await using var oversized = new MemoryStream();
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, ActivationPipeProtocol.MaximumMessageBytes + 1);
        await oversized.WriteAsync(header);
        oversized.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => ActivationPipeProtocol.ReadRequestAsync(oversized, default));
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public async Task InvalidWirePayload_IsRejected(string _, byte[] payload) =>
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadPayloadAsync(payload));

    public static IEnumerable<object[]> InvalidPayloads()
    {
        var id = Guid.NewGuid();
        var json = $"{{\"ProtocolVersion\":1,\"RequestId\":\"{id}\",\"Command\":0,\"Source\":0,\"Anchor\":null,\"Edge\":null}}";
        yield return ["invalid JSON", Encoding.UTF8.GetBytes("{not-json")];
        yield return ["invalid UTF-8", new byte[] { 0xff, 0xfe, 0xfd }];
        yield return ["unsupported version", Utf8(json.Replace("\"ProtocolVersion\":1", "\"ProtocolVersion\":2"))];
        yield return ["unknown command", Utf8(json.Replace("\"Command\":0", "\"Command\":999"))];
        yield return ["unknown source", Utf8(json.Replace("\"Source\":0", "\"Source\":999"))];
        yield return ["unknown edge", Utf8(json.Replace("\"Edge\":null", "\"Edge\":999"))];
        yield return ["missing field", Utf8(json.Replace(",\"Edge\":null", ""))];
        yield return ["duplicate field", Utf8(json.Replace("{", "{\"ProtocolVersion\":1,"))];
        yield return ["unknown field", Utf8(json.Insert(json.Length - 1, ",\"Extra\":true"))];
        yield return ["empty request id", Utf8(json.Replace(id.ToString(), Guid.Empty.ToString()))];
        yield return ["null document", Utf8("null")];
        yield return ["empty object", Utf8("{}")];
        yield return ["invalid request id", Utf8(json.Replace(id.ToString(), "not-a-guid"))];
        yield return ["null command", Utf8(json.Replace("\"Command\":0", "\"Command\":null"))];
        yield return ["string command", Utf8(json.Replace("\"Command\":0", "\"Command\":\"Show\""))];
        yield return ["orphan edge", Utf8(json.Replace("\"Edge\":null", "\"Edge\":3"))];

        var integrated = json.Replace("\"Source\":0", "\"Source\":2")
            .Replace("\"Anchor\":null", "\"Anchor\":{\"Left\":-10,\"Top\":0,\"Right\":10,\"Bottom\":20}")
            .Replace("\"Edge\":null", "\"Edge\":3");
        yield return ["nested missing field", Utf8(integrated.Replace(",\"Bottom\":20", ""))];
        yield return ["nested duplicate field", Utf8(integrated.Replace("\"Left\":-10", "\"Left\":-10,\"Left\":-20"))];
        yield return ["nested extra field", Utf8(integrated.Replace("\"Left\":-10", "\"Left\":-10,\"Width\":20"))];
        yield return ["orphan anchor", Utf8(integrated.Replace("\"Edge\":3", "\"Edge\":null"))];
        yield return ["placement on simple source", Utf8(integrated.Replace("\"Source\":2", "\"Source\":0"))];
        yield return ["placement on hide", Utf8(integrated.Replace("\"Command\":0", "\"Command\":2"))];
    }

    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);
    private static async Task<ActivationRequest> RoundTripAsync(ActivationRequest request)
    {
        await using var stream = new MemoryStream();
        await ActivationPipeProtocol.WriteRequestAsync(stream, request, default);
        stream.Position = 0;
        return await ActivationPipeProtocol.ReadRequestAsync(stream, default);
    }
    private static async Task<string> WriteAndExtractJsonAsync(ActivationRequest request)
    {
        await using var stream = new MemoryStream();
        await ActivationPipeProtocol.WriteRequestAsync(stream, request, default);
        var bytes = stream.ToArray();
        var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, sizeof(int)));
        return Encoding.UTF8.GetString(bytes, sizeof(int), length);
    }
    private static async Task<ActivationRequest> ReadPayloadAsync(byte[] payload)
    {
        await using var stream = new MemoryStream();
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header);
        await stream.WriteAsync(payload);
        stream.Position = 0;
        return await ActivationPipeProtocol.ReadRequestAsync(stream, default);
    }
}
