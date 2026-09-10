using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using EmberStart.Core.Activation;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;

namespace EmberStart.ActivationFixture;

public static class ActivationFixtureMarker;

// This is a controlled transport fixture, not the WPF resident or a recovery implementation.
public sealed record FixtureEvent(string Event, int ProcessId, bool? IsPrimary = null,
    Guid? RequestId = null, bool? Accepted = null, string? Code = null, double? ElapsedMilliseconds = null,
    string? Stage = null, bool? TransmissionStarted = null);

public static class Program
{
    private static readonly object OutputGate = new();

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1 || !Guid.TryParseExact(args[0], "N", out var namespaceId))
        {
            return 2;
        }

        if (!ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident)
        {
            Emit("error", code: "MediumIntegrityRequired");
            return 3;
        }

        var current = CurrentSessionIdentity.Create();
        var suffix = namespaceId.ToString("N", CultureInfo.InvariantCulture);
        var identity = current with
        {
            Names = new InstanceIdentity($"Local\\EmberStart.ColdProcess.{suffix}",
                $"EmberStart.ColdProcess.{suffix}")
        };
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var delayedHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SingleInstanceCoordinator? coordinator = null;
        try
        {
            Emit("booted");
            while (await Console.In.ReadLineAsync(lifetime.Token).ConfigureAwait(false) is { } line)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return 2;
                }

                switch (parts[0])
                {
                    case "create" when parts.Length == 1 && coordinator is null:
                        coordinator = SingleInstanceCoordinator.Create(identity);
                        Emit("created", coordinator.IsPrimary);
                        break;
                    case "probe-held" when parts.Length == 1 && coordinator is not null:
                        using (var probe = SingleInstanceCoordinator.Create(identity))
                        {
                            Emit("probed", probe.IsPrimary);
                        }
                        break;
                    case "release" when parts.Length == 1 && coordinator is not null:
                        coordinator.Dispose();
                        coordinator = null;
                        Emit("released");
                        break;
                    case "listen" or "listen-delayed" when parts.Length == 1 && coordinator is not null:
                        var delayed = parts[0] == "listen-delayed";
                        coordinator.StartListening(async (request, _) =>
                        {
                            Emit("applied", requestId: request.RequestId);
                            if (delayed)
                            {
                                // Deliberately ignore cancellation: receiving no ACK does not undo application.
                                await delayedHandler.Task.ConfigureAwait(false);
                            }
                            Emit("handler-finished", requestId: request.RequestId);
                            return new ActivationResponse(request.RequestId, true, "Accepted");
                        });
                        Emit("listening", coordinator.IsPrimary, accepted: await coordinator.Ready.ConfigureAwait(false),
                            code: coordinator.ListenerHealth.ToString());
                        break;
                    case "finish-handler" when parts.Length == 1:
                        delayedHandler.TrySetResult();
                        Emit("released-handler");
                        break;
                    case "send" when parts.Length == 2 && coordinator is { IsPrimary: false } &&
                        Guid.TryParseExact(parts[1], "N", out var requestId):
                        await SendAsync(coordinator, requestId, lifetime.Token).ConfigureAwait(false);
                        break;
                    case "stop" when parts.Length == 1:
                        return 0;
                    default:
                        Emit("error", code: "InvalidControlCommand");
                        return 2;
                }
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            Emit("error", code: "FixtureDeadline");
            return 4;
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
        {
            // Do not expose local paths, SIDs, or arbitrary exception text in structural evidence.
            Emit("error", code: exception.GetType().Name);
            return 5;
        }
        finally
        {
            delayedHandler.TrySetResult();
            coordinator?.Dispose();
        }
    }

    private static async Task SendAsync(SingleInstanceCoordinator coordinator, Guid requestId,
        CancellationToken cancellationToken)
    {
        var request = ActivationRequest.CreateSimple(ActivationCommand.Toggle, ActivationSource.CommandLine)
            with
        { RequestId = requestId };
        var timer = Stopwatch.StartNew();
        Emit("sending", requestId: requestId);
        try
        {
            var response = await coordinator.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Emit("result", requestId: response.RequestId, accepted: response.Accepted, code: response.Code,
                elapsedMilliseconds: timer.Elapsed.TotalMilliseconds);
        }
        catch (ActivationSendException exception)
        {
            // Surface the stage so tests observe whether transmission may have begun.
            Emit("result", requestId: requestId, accepted: false,
                code: exception.InnerException?.GetType().Name ?? exception.GetType().Name,
                elapsedMilliseconds: timer.Elapsed.TotalMilliseconds, stage: exception.Stage.ToString(),
                transmissionStarted: exception.TransmissionStarted);
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or
            OperationCanceledException or UnauthorizedAccessException)
        {
            Emit("result", requestId: requestId, accepted: false, code: exception.GetType().Name,
                elapsedMilliseconds: timer.Elapsed.TotalMilliseconds);
        }
    }

    private static void Emit(string name, bool? isPrimary = null, Guid? requestId = null,
        bool? accepted = null, string? code = null, double? elapsedMilliseconds = null,
        string? stage = null, bool? transmissionStarted = null)
    {
        lock (OutputGate)
        {
            Console.WriteLine(JsonSerializer.Serialize(new FixtureEvent(name, Environment.ProcessId,
                isPrimary, requestId, accepted, code, elapsedMilliseconds, stage, transmissionStarted)));
        }
    }
}
