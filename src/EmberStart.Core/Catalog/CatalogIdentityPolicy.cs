namespace EmberStart.Core.Catalog;

public static class CatalogIdentityPolicy
{
    public static IReadOnlyList<CatalogEntry> Normalize(IEnumerable<CatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<CatalogEntry>();

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                continue;
            }

            var key = string.IsNullOrWhiteSpace(entry.AppUserModelId)
                ? $"shell:{entry.Id}"
                : $"aumid:{entry.AppUserModelId}";
            if (seen.Add(key))
            {
                normalized.Add(entry);
            }
        }

        return normalized
            .OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
