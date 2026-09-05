using System.Buffers.Binary;
using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

/// <summary>Deterministic protocol fuzzing; listener recovery has its own fixture.</summary>
public sealed class Edd01AdversaryCorpusTests
{
    [Fact]
    public async Task TenThousandInvalidUtf8Frames_AreRejectedAsInvalidData()
    {
        var random = new Random(0xEDD03);
        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            var payload = new byte[random.Next(1, 257)];
            random.NextBytes(payload);
            payload[0] = 0xff;
            await using var stream = new MemoryStream(sizeof(int) + payload.Length);
            var header = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
            await stream.WriteAsync(header);
            await stream.WriteAsync(payload);
            stream.Position = 0;
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                ActivationPipeProtocol.ReadRequestAsync(stream, CancellationToken.None));
        }
    }
}
