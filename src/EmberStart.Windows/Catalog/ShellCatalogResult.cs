using EmberStart.Core.Catalog;

namespace EmberStart.Windows.Catalog;

public sealed record ShellCatalogResult(
    IReadOnlyList<CatalogEntry> Entries,
    ShellCatalogStatus Status);
