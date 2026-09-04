using System.Collections.Concurrent;

namespace EmberStart.Windows.Catalog;

internal sealed class StaShellWorker : IDisposable
{
    private const int QueueCapacity = 64;

    private readonly BlockingCollection<Action> _queue = new(
        new ConcurrentQueue<Action>(),
        QueueCapacity);
    private readonly Thread _thread;
    private int _circuitOpen;
    private int _disposed;

    public StaShellWorker()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "Ember Start Shell STA",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public Task<T> InvokeAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _circuitOpen) != 0)
        {
            completion.SetException(new InvalidOperationException("The Shell worker is unavailable."));
            return completion.Task;
        }

        try
        {
            if (!_queue.TryAdd(() => Execute(operation, completion)))
            {
                completion.SetException(new InvalidOperationException("The Shell worker queue is full."));
            }
        }
        catch (InvalidOperationException)
        {
            completion.SetException(new InvalidOperationException("The Shell worker is unavailable."));
        }

        return completion.Task;
    }

    public void OpenCircuit()
    {
        if (Interlocked.Exchange(ref _circuitOpen, 1) == 0)
        {
            _queue.CompleteAdding();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _queue.CompleteAdding();
        if (_thread.Join(TimeSpan.FromMilliseconds(250)))
        {
            _queue.Dispose();
        }
    }

    private void Run()
    {
        foreach (var work in _queue.GetConsumingEnumerable())
        {
            work();
        }
    }

    private void Execute<T>(Func<T> operation, TaskCompletionSource<T> completion)
    {
        if (Volatile.Read(ref _circuitOpen) != 0)
        {
            completion.TrySetException(new InvalidOperationException("The Shell worker circuit is open."));
            return;
        }

        try
        {
            completion.TrySetResult(operation());
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }
}
