using EmberStart.Core.Catalog;

namespace EmberStart.Windows.Catalog;

public sealed class ShellAppService : IDisposable
{
    public static readonly TimeSpan InitialCatalogTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan IncrementalItemTimeout = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(2);

    private readonly StaShellWorker _worker = new();
    private bool _disposed;

    public async Task<ShellCatalogResult> LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var work = _worker.InvokeAsync(ShellNativeCatalog.Enumerate);

        try
        {
            var entries = await work
                .WaitAsync(InitialCatalogTimeout, cancellationToken)
                .ConfigureAwait(false);
            return new ShellCatalogResult(entries, ShellCatalogStatus.Ready);
        }
        catch (TimeoutException)
        {
            _worker.OpenCircuit();
            ObserveLateResult(work);
            return new ShellCatalogResult([], ShellCatalogStatus.TimedOut);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObserveLateResult(work);
            throw;
        }
        catch (Exception exception) when (IsExpectedShellFailure(exception))
        {
            return new ShellCatalogResult([], ShellCatalogStatus.Unavailable);
        }
    }

    public async Task<ShellIconHandle?> LoadIconAsync(
        CatalogEntry entry,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);
        var work = _worker.InvokeAsync(() => ShellNativeCatalog.GetIcon(entry));

        try
        {
            return await work
                .WaitAsync(IncrementalItemTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _worker.OpenCircuit();
            ObserveLateResult(work, icon => icon?.Dispose());
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObserveLateResult(work, icon => icon?.Dispose());
            throw;
        }
        catch (Exception exception) when (IsExpectedShellFailure(exception))
        {
            return null;
        }
    }

    public async Task<ShellLaunchResult> LaunchAsync(
        CatalogEntry entry,
        nint ownerWindow,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Kind == CatalogEntryKind.Unknown)
        {
            return ShellLaunchResult.Failure("UnsupportedIdentity");
        }

        var work = _worker.InvokeAsync(() => ShellNativeCatalog.Launch(entry, ownerWindow));

        try
        {
            return await work
                .WaitAsync(LaunchTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _worker.OpenCircuit();
            ObserveLateResult(work);
            return ShellLaunchResult.Failure("TimedOut");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObserveLateResult(work);
            throw;
        }
        catch (Exception exception) when (IsExpectedShellFailure(exception))
        {
            return ShellLaunchResult.Failure("LaunchFailed");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _worker.Dispose();
        GC.SuppressFinalize(this);
    }

    private static bool IsExpectedShellFailure(Exception exception) =>
        exception is System.ComponentModel.Win32Exception or
            System.Runtime.InteropServices.COMException or
            ArgumentException or
            IOException or
            InvalidOperationException or
            System.Security.SecurityException or
            UnauthorizedAccessException;

    private static void ObserveLateResult<T>(Task<T> task, Action<T>? onSuccess = null)
    {
        _ = task.ContinueWith(
            completed =>
            {
                if (completed.Status == TaskStatus.RanToCompletion)
                {
                    onSuccess?.Invoke(completed.Result);
                }
                else if (completed.IsFaulted)
                {
                    _ = completed.Exception;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
