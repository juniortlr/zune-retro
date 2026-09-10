namespace EmberStart.Windows.Catalog;

public sealed record ShellLaunchResult(bool Succeeded, string StatusCode)
{
    public static ShellLaunchResult Success() => new(true, "Launched");

    public static ShellLaunchResult Failure(string statusCode) => new(false, statusCode);
}
