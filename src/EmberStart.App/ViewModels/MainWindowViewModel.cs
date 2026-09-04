using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EmberStart.Core.Catalog;

namespace EmberStart.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly CatalogEntry[] _catalog =
    [
        new("preview.calculator", "Calculator"),
        new("preview.documents", "Documents"),
        new("preview.explorer", "File Explorer"),
        new("preview.music", "Music"),
        new("preview.photos", "Photos"),
        new("preview.settings", "Settings"),
        new("preview.terminal", "Terminal"),
        new("preview.store", "Microsoft Store"),
    ];

    private string _searchText = string.Empty;
    private string _statusText = "Foundation preview · catalog adapter next";

    public MainWindowViewModel()
    {
        RefreshResults();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CatalogEntry> Results { get; } = [];

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

    public void ReportHotKeyUnavailable() =>
        StatusText = "Ctrl+Alt+Space is unavailable · use EmberStart.exe --toggle";

    private void RefreshResults()
    {
        Results.Clear();
        foreach (var item in AppNameFilter.Filter(_catalog, SearchText))
        {
            Results.Add(item);
        }

        StatusText = Results.Count == 0
            ? "No apps found"
            : $"{Results.Count} apps found";
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
