namespace EmberStart.Core.Activation;

public sealed record ActivationParseResult(ActivationRequest? Request, string? Error)
{
    public bool Success => Request is not null;

    public static ActivationParseResult Accepted(ActivationRequest request) => new(request, null);

    public static ActivationParseResult Rejected(string error) => new(null, error);
}
