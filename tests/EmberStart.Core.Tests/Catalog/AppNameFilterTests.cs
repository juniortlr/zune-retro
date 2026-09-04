using EmberStart.Core.Catalog;

namespace EmberStart.Core.Tests.Catalog;

public sealed class AppNameFilterTests
{
    private static readonly CatalogEntry[] Entries =
    [
        new("3", "Visual Studio Code"),
        new("1", "Code Writer"),
        new("2", "Barcode Tool"),
    ];

    [Fact]
    public void Filter_RanksExactThenTokenThenSubstring()
    {
        var results = AppNameFilter.Filter(Entries, "code");

        Assert.Equal(["Code Writer", "Visual Studio Code", "Barcode Tool"], results.Select(x => x.DisplayName));
    }

    [Fact]
    public void Filter_EmptyQueryHasDeterministicOrder()
    {
        var results = AppNameFilter.Filter(Entries, string.Empty);

        Assert.Equal(["Barcode Tool", "Code Writer", "Visual Studio Code"], results.Select(x => x.DisplayName));
    }
}
