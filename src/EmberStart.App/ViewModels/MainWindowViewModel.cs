using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using EmberStart.Core.Catalog;
using EmberStart.Windows.Catalog;

namespace EmberStart.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ShellAppService _shellApps;
    private readonly List<CatalogItemViewModel> _catalog = [];

    private string _searchText = string.Empty;
    private string _statusText = "Loading installed apps…";
    private ShellCatalogStatus? _catalogStatus;
    private bool _hotKeyUnavailable;
    private bool _initialized;

    public MainWindowViewModel(ShellAppService shellApps)
    {
        ArgumentNullException.ThrowIfNull(shellApps);
        _shellApps = shellApps;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CatalogItemViewModel> Results { get; } = [];

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetField(ref _searchText, value))
            {
                return;
            }

            RefreshResults();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        StatusText = "Loading installed apps…";
        var result = await _shellApps.LoadCatalogAsync(cancellationToken);
        _catalogStatus = result.Status;
        _catalog.Clear();
        _catalog.AddRange(result.Entries.Select(entry => new CatalogItemViewModel(entry)));
        RefreshResults();

        if (result.Status == ShellCatalogStatus.Ready)
        {
            await LoadIconsAsync(cancellationToken);
        }
    }

    public void ReportHotKeyUnavailable()
    {
        _hotKeyUnavailable = true;
        RefreshStatus();
    }

    public async Task<bool> LaunchAsync(
        CatalogItemViewModel item,
        nint ownerWindow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        StatusText = $"Opening {item.DisplayName}…";
        var result = await _shellApps.LaunchAsync(item.Entry, ownerWindow, cancellationToken);
        StatusText = result.Succeeded
            ? $"Opened {item.DisplayName}"
            : $"Could not open {item.DisplayName}";
        return result.Succeeded;
    }

    private void RefreshResults()
    {
        Results.Clear();
        var byIdentity = _catalog.ToDictionary(item => item.Entry.Id, StringComparer.Ordinal);
        foreach (var entry in AppNameFilter.Filter(_catalog.Select(item => item.Entry), SearchText))
        {
            Results.Add(byIdentity[entry.Id]);
        }

        RefreshStatus();
    }

    private async Task LoadIconsAsync(CancellationToken cancellationToken)
    {
        foreach (var item in _catalog)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var icon = await _shellApps.LoadIconAsync(item.Entry, cancellationToken);
            if (icon is null)
            {
                continue;
            }

            var image = Imaging.CreateBitmapSourceFromHIcon(
                icon.DangerousGetHandle(),
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            image.Freeze();
            item.Icon = image;
        }
    }

    private void RefreshStatus()
    {
        var catalogText = _catalogStatus switch
        {
            null => "Loading installed apps…",
            ShellCatalogStatus.TimedOut => "App catalog timed out · native Start remains available",
            ShellCatalogStatus.Unavailable => "App catalog unavailable · native Start remains available",
            _ when Results.Count == 0 && SearchText.Length > 0 => "No apps found",
            _ => $"{Results.Count} installed apps",
        };

        StatusText = _hotKeyUnavailable
            ? $"{catalogText} · Ctrl+Alt+Space unavailable"
            : catalogText;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
