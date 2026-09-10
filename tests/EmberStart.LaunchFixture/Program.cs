namespace EmberStart.LaunchFixture;

public static class LaunchFixtureMarker;

public static class Program
{
    public static int Main()
    {
        var output = Environment.GetEnvironmentVariable("EMBERSTART_FIXTURE_OUTPUT");
        var nonce = Environment.GetEnvironmentVariable("EMBERSTART_FIXTURE_NONCE");
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(nonce))
        {
            return 2;
        }

        var fullOutput = Path.GetFullPath(output);
        var directory = Path.GetDirectoryName(fullOutput);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return 3;
        }

        File.AppendAllText(fullOutput, $"{nonce}{Environment.NewLine}");
        return 0;
    }
}
