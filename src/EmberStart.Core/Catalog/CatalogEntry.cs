namespace EmberStart.Core.Catalog;

public sealed record CatalogEntry(
    string Id,
    string DisplayName,
    CatalogEntryKind Kind = CatalogEntryKind.Unknown,
    string? AppUserModelId = null);
