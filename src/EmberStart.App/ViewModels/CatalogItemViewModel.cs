using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using EmberStart.Core.Catalog;

namespace EmberStart.App.ViewModels;

public sealed class CatalogItemViewModel : INotifyPropertyChanged
{
    private ImageSource? _icon;

    public CatalogItemViewModel(CatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CatalogEntry Entry { get; }

    public string DisplayName => Entry.DisplayName;

    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
            {
                return;
            }

            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }
}
