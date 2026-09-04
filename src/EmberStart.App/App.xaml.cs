using System.IO;
using System.Windows;
using EmberStart.Core.Activation;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;

namespace EmberStart.App;

public partial class App : Application, IDisposable
{
    private SingleInstanceCoordinator? _coordinator;
    private MainWindow? _menu;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var parsed = ActivationCommandParser.Parse(e.Args);
        if (!parsed.Success)
        {
            MessageBox.Show(
                parsed.Error,
                "Ember Start",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(2);
            return;
        }

        var integrity = ProcessIntegrityGuard.EvaluateCurrentProcess();
        if (!integrity.MayBecomeResident)
        {
            MessageBox.Show(
                integrity.Message,
                "Ember Start",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(5);
            return;
        }

        var identity = CurrentSessionIdentity.Create();
        _coordinator = SingleInstanceCoordinator.Create(identity);
        if (!_coordinator.IsPrimary)
        {
            await RedirectToPrimaryAsync(parsed.Request!).ConfigureAwait(true);
            return;
        }

        _menu = new MainWindow(ApplyActivation);
        MainWindow = _menu;
        _menu.InitializeNativeWindow();
        _coordinator.StartListening(HandlePipeActivationAsync);
        _menu.ApplyActivation(parsed.Request!);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        _menu?.Dispose();
        _coordinator?.Dispose();
        _menu = null;
        _coordinator = null;
        GC.SuppressFinalize(this);
    }

    private async Task RedirectToPrimaryAsync(ActivationRequest request)
    {
        try
        {
            var response = await _coordinator!.SendAsync(request).ConfigureAwait(true);
            Shutdown(response.Accepted ? 0 : 4);
        }
        catch (Exception exception) when (
            exception is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                "Ember Start could not reach its existing session. Native Start remains available with Ctrl+Esc.",
                "Ember Start",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(4);
        }
    }

    private void ApplyActivation(ActivationRequest request) => _menu?.ApplyActivation(request);

    private async Task<ActivationResponse> HandlePipeActivationAsync(
        ActivationRequest request,
        CancellationToken cancellationToken)
    {
        await Dispatcher.InvokeAsync(
            () => _menu?.ApplyActivation(request),
            System.Windows.Threading.DispatcherPriority.Send,
            cancellationToken);

        return new ActivationResponse(request.RequestId, true, "Accepted");
    }
}
