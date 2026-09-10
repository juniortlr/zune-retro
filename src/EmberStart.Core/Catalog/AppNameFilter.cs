namespace EmberStart.Core.Catalog;

public static class AppNameFilter
{
    public static IReadOnlyList<CatalogEntry> Filter(
        IEnumerable<CatalogEntry> entries,
        string? query,
        int maximumResults = 100)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumResults);

        var normalizedQuery = query?.Trim() ?? string.Empty;

        return entries
            .Select(entry => new { Entry = entry, Rank = GetRank(entry.DisplayName, normalizedQuery) })
            .Where(result => result.Rank < int.MaxValue)
            .OrderBy(result => result.Rank)
            .ThenBy(result => result.Entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(result => result.Entry.Id, StringComparer.Ordinal)
            .Take(maximumResults)
            .Select(result => result.Entry)
            .ToArray();
    }

    private static int GetRank(string displayName, string query)
    {
        if (query.Length == 0)
        {
            return 0;
        }

        if (displayName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 0;
        }

        if (displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => token.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)))
        {
            return 1;
        }

        return displayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            ? 2
            : int.MaxValue;
    }
}
