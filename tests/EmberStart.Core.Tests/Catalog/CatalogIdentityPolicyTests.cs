using EmberStart.Core.Catalog;

namespace EmberStart.Core.Tests.Catalog;

public sealed class CatalogIdentityPolicyTests
{
    [Fact]
    public void Normalize_PrefersFirstEntryWithSameAumid()
    {
        CatalogEntry[] entries =
        [
            new("apps:first", "First", CatalogEntryKind.Packaged, "Publisher.App_123!App"),
            new("shortcut:duplicate", "Duplicate", CatalogEntryKind.ShellItem, "publisher.app_123!app"),
        ];

        var result = CatalogIdentityPolicy.Normalize(entries);

        Assert.Collection(result, entry => Assert.Equal("apps:first", entry.Id));
    }

    [Fact]
    public void Normalize_DeduplicatesCanonicalShellIdentityWithoutUsingDisplayName()
    {
        CatalogEntry[] entries =
        [
            new("shell:one", "Same name", CatalogEntryKind.ShellItem),
            new("SHELL:ONE", "Renamed duplicate", CatalogEntryKind.ShellItem),
            new("shell:two", "Same name", CatalogEntryKind.ShellItem),
        ];

        var result = CatalogIdentityPolicy.Normalize(entries);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, entry => entry.Id == "shell:one");
        Assert.Contains(result, entry => entry.Id == "shell:two");
    }

    [Fact]
    public void Normalize_DropsBlankIdentityOrName()
    {
        CatalogEntry[] entries =
        [
            new(string.Empty, "Missing identity"),
            new("valid", " "),
            new("valid", "Valid"),
        ];

        var result = CatalogIdentityPolicy.Normalize(entries);

        Assert.Collection(result, entry => Assert.Equal("Valid", entry.DisplayName));
    }
}
