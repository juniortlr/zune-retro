using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using EmberStart.App.ViewModels;
using EmberStart.Core.Activation;
using EmberStart.Windows.Activation;
using EmberStart.Windows.Catalog;
using EmberStart.Windows.Display;

namespace EmberStart.App;

public partial class MainWindow : Window, IDisposable
{
    private readonly Action<ActivationRequest> _activationCallback;
    private readonly GlobalHotKeyRegistration _hotKey = new();
    private readonly ShellAppService _shellApps = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly MainWindowViewModel _viewModel;
    private HwndSource? _source;
    private nint _windowHandle;
    private bool _disposed;

    public MainWindow(Action<ActivationRequest> activationCallback)
    {
        ArgumentNullException.ThrowIfNull(activationCallback);
        _activationCallback = activationCallback;
        _viewModel = new MainWindowViewModel(_shellApps);
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void InitializeNativeWindow()
    {
        if (_windowHandle != nint.Zero)
        {
            return;
        }

        _windowHandle = new WindowInteropHelper(this).EnsureHandle();
        _source = HwndSource.FromHwnd(_windowHandle);
        _source.AddHook(WindowProcedure);

        if (!_hotKey.TryRegister(_windowHandle))
        {
            _viewModel.ReportHotKeyUnavailable();
        }
    }

    public void ApplyActivation(ActivationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        InitializeNativeWindow();

        var shouldShow = MenuVisibilityPolicy.GetExpectedVisibility(IsVisible, request.Command);
        if (!shouldShow)
        {
            Hide();
            return;
        }

        _ = WindowsMonitorPlacement.Place(_windowHandle, request);
        Show();
        Activate();
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        _source?.RemoveHook(WindowProcedure);
        _hotKey.Dispose();
        _shellApps.Dispose();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }

    private nint WindowProcedure(
        nint window,
        int message,
        nint wordParameter,
        nint longParameter,
        ref bool handled)
    {
        _ = window;
        _ = longParameter;
        if (!GlobalHotKeyRegistration.IsHotKeyMessage(message, wordParameter))
        {
            return nint.Zero;
        }

        handled = true;
        _activationCallback(new ActivationRequest(
            ActivationRequest.CurrentProtocolVersion,
            Guid.NewGuid(),
            ActivationCommand.Toggle,
            ActivationSource.HotKey,
            null,
            null));

        return nint.Zero;
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs eventArgs)
    {
        _ = sender;
        if (eventArgs.Key == Key.Escape)
        {
            Hide();
            eventArgs.Handled = true;
            return;
        }

        if (eventArgs.Key == Key.Down && SearchBox.IsKeyboardFocusWithin && ResultsList.Items.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
            ResultsList.Focus();
            eventArgs.Handled = true;
            return;
        }

        if (eventArgs.Key != Key.Enter ||
            (!SearchBox.IsKeyboardFocusWithin && !ResultsList.IsKeyboardFocusWithin) ||
            ResultsList.Items.Count == 0)
        {
            return;
        }

        if (ResultsList.SelectedItem is null)
        {
            ResultsList.SelectedIndex = 0;
        }

        eventArgs.Handled = true;
        await LaunchSelectedAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;

        try
        {
            await _viewModel.InitializeAsync(_lifetime.Token);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
    }

    private async void OnResultsMouseDoubleClick(object sender, MouseButtonEventArgs eventArgs)
    {
        _ = sender;
        if (eventArgs.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(ResultsList, source) is not ListBoxItem)
        {
            return;
        }

        eventArgs.Handled = true;
        await LaunchSelectedAsync();
    }

    private async Task LaunchSelectedAsync()
    {
        if (ResultsList.SelectedItem is not CatalogItemViewModel item)
        {
            return;
        }

        try
        {
            if (await _viewModel.LaunchAsync(item, _windowHandle, _lifetime.Token))
            {
                Hide();
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
    }

    private void OnDeactivated(object? sender, EventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        Hide();
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        _ = sender;
        _ = eventArgs;
        Dispose();
    }
}
