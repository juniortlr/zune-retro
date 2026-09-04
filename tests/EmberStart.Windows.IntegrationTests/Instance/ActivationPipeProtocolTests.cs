using EmberStart.Core.Activation;
using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationPipeProtocolTests
{
    [Fact]
    public async Task Request_RoundTripsThroughBoundedFrame()
    {
        var request = ActivationRequest.CreateSimple(ActivationCommand.Toggle, ActivationSource.CommandLine);
        await using var stream = new MemoryStream();

        await ActivationPipeProtocol.WriteRequestAsync(stream, request, CancellationToken.None);
        stream.Position = 0;
        var restored = await ActivationPipeProtocol.ReadRequestAsync(stream, CancellationToken.None);

        Assert.Equal(request, restored);
        Assert.True(stream.Length <= ActivationPipeProtocol.MaximumMessageBytes + sizeof(int));
    }

    [Fact]
    public async Task Read_RejectsFrameOverFourKiB()
    {
        await using var stream = new MemoryStream();
        var header = BitConverter.GetBytes(ActivationPipeProtocol.MaximumMessageBytes + 1);
        await stream.WriteAsync(header, CancellationToken.None);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            ActivationPipeProtocol.ReadRequestAsync(stream, CancellationToken.None));
    }
}
