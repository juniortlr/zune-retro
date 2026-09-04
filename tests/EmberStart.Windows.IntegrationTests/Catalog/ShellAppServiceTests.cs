using EmberStart.Core.Catalog;
using EmberStart.LaunchFixture;
using EmberStart.Windows.Catalog;

namespace EmberStart.Windows.IntegrationTests.Catalog;

public sealed class ShellAppServiceTests
{
    [Fact]
    public async Task Catalog_ContainsWellFormedUniqueEntriesWhenShellIsAvailable()
    {
        var catalogIsRequired = Environment.GetEnvironmentVariable("EMBERSTART_REQUIRE_SHELL") == "1";
        using var service = new ShellAppService();

        var result = await service.LoadCatalogAsync(CancellationToken.None);
        if (result.Status != ShellCatalogStatus.Ready)
        {
            Assert.False(catalogIsRequired, $"Shell catalog status was {result.Status}.");
            return;
        }

        Assert.NotEmpty(result.Entries);
        Assert.All(result.Entries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Id));
            Assert.False(string.IsNullOrWhiteSpace(entry.DisplayName));
            Assert.NotEqual(CatalogEntryKind.Unknown, entry.Kind);
        });
        Assert.Equal(
            result.Entries.Count,
            result.Entries.Select(entry => entry.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        if (catalogIsRequired)
        {
            Assert.Contains(result.Entries, entry => entry.Kind == CatalogEntryKind.ShellItem);
            Assert.Contains(result.Entries, entry => entry.Kind == CatalogEntryKind.Packaged);

            ShellIconHandle? icon = null;
            foreach (var entry in result.Entries.Take(20))
            {
                icon = await service.LoadIconAsync(entry, CancellationToken.None);
                if (icon is not null)
                {
                    break;
                }
            }

            Assert.NotNull(icon);
            icon.Dispose();
        }
    }

    [Fact]
    public async Task Launch_ExecutesClassicFixtureExactlyOnceWithoutArguments()
    {
        var fixturePath = Path.ChangeExtension(typeof(LaunchFixtureMarker).Assembly.Location, ".exe");
        Assert.True(File.Exists(fixturePath), "The classic launch fixture apphost was not copied to the test output.");

        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            "EmberStart.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        var outputPath = Path.Combine(testDirectory, "launch.txt");
        var nonce = Guid.NewGuid().ToString("N");
        var previousOutput = Environment.GetEnvironmentVariable("EMBERSTART_FIXTURE_OUTPUT");
        var previousNonce = Environment.GetEnvironmentVariable("EMBERSTART_FIXTURE_NONCE");

        try
        {
            Environment.SetEnvironmentVariable("EMBERSTART_FIXTURE_OUTPUT", outputPath);
            Environment.SetEnvironmentVariable("EMBERSTART_FIXTURE_NONCE", nonce);
            using var service = new ShellAppService();
            var entry = new CatalogEntry(fixturePath, "Classic fixture", CatalogEntryKind.ShellItem);

            var launch = await service.LaunchAsync(
                entry,
                ownerWindow: nint.Zero,
                CancellationToken.None);

            Assert.True(launch.Succeeded, launch.StatusCode);
            await WaitForFileAsync(outputPath, CancellationToken.None);
            Assert.Equal(
                [nonce],
                await File.ReadAllLinesAsync(outputPath, CancellationToken.None));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMBERSTART_FIXTURE_OUTPUT", previousOutput);
            Environment.SetEnvironmentVariable("EMBERSTART_FIXTURE_NONCE", previousNonce);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Launch_RejectsUnknownPackagedIdentityWithoutStartingAnything()
    {
        using var service = new ShellAppService();
        var entry = new CatalogEntry(
            "shell:AppsFolder\\EmberStart.InvalidPackage_000!Missing",
            "Invalid packaged fixture",
            CatalogEntryKind.Packaged,
            "EmberStart.InvalidPackage_000!Missing");

        var result = await service.LaunchAsync(entry, nint.Zero, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("LaunchFailed", result.StatusCode);
    }

    private static async Task WaitForFileAsync(string path, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!File.Exists(path) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, cancellationToken);
        }

        Assert.True(File.Exists(path), "The classic launch fixture did not report activation.");
    }
}
