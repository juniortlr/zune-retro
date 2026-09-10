using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using EmberStart.ActivationFixture;
using Xunit.Abstractions;

namespace EmberStart.Windows.IntegrationTests.Instance;

internal sealed class ActivationFixtureProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Channel<FixtureEvent> _events = Channel.CreateUnbounded<FixtureEvent>();
    private readonly ConcurrentQueue<FixtureEvent> _observed = new();
    private readonly Task _reader;
    private readonly Task<string> _errors;

    private ActivationFixtureProcess(Process process, ITestOutputHelper output)
    {
        _process = process;
        _errors = process.StandardError.ReadToEndAsync();
        _reader = ReadOutputAsync(output);
    }

    public int Id => _process.Id;
    public FixtureEvent[] Observed => _observed.ToArray();

    public static async Task<ActivationFixtureProcess> StartAsync(Guid namespaceId, ITestOutputHelper output)
    {
        var executable = Path.ChangeExtension(typeof(ActivationFixtureMarker).Assembly.Location, ".exe");
        Assert.True(File.Exists(executable), "Activation fixture apphost must be copied to the test output.");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(namespaceId.ToString("N"));
        var process = Process.Start(start) ?? throw new InvalidOperationException("Fixture failed to start.");
        var fixture = new ActivationFixtureProcess(process, output);
        try
        {
            await fixture.ReadAsync("booted");
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public async Task WriteAsync(string command)
    {
        await _process.StandardInput.WriteLineAsync(command);
        await _process.StandardInput.FlushAsync();
    }

    public async Task<FixtureEvent> CommandAsync(string command, string expectedEvent)
    {
        await WriteAsync(command);
        return await ReadAsync(expectedEvent);
    }

    public async Task<FixtureEvent> ReadAsync(string expectedEvent)
    {
        // Fixture orchestration deadline; production transport deadlines remain 500 ms.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (true)
        {
            var observed = await _events.Reader.ReadAsync(deadline.Token);
            Assert.Equal(Id, observed.ProcessId);
            Assert.NotEqual("error", observed.Event);
            if (observed.Event == expectedEvent)
            {
                return observed;
            }
        }
    }

    public async Task StopAsync()
    {
        await WriteAsync("stop");
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await _reader;
        Assert.Equal(0, _process.ExitCode);
        Assert.True(string.IsNullOrEmpty(await _errors), "Fixture wrote unexpected stderr.");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                try
                {
                    await WriteAsync("stop");
                    await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (Exception exception) when (exception is IOException or InvalidOperationException or TimeoutException)
                {
                    // Only this explicitly created fixture process is eligible for forced cleanup.
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }
            }
            await _reader;
        }
        finally
        {
            _process.Dispose();
        }
    }

    private async Task ReadOutputAsync(ITestOutputHelper output)
    {
        try
        {
            while (await _process.StandardOutput.ReadLineAsync() is { } line)
            {
                var observed = JsonSerializer.Deserialize<FixtureEvent>(line)
                    ?? throw new InvalidDataException("Fixture event is missing.");
                _observed.Enqueue(observed);
                output.WriteLine(line);
                await _events.Writer.WriteAsync(observed);
            }
            _events.Writer.TryComplete();
        }
        catch (Exception exception)
        {
            _events.Writer.TryComplete(exception);
        }
    }
}
