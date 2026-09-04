using System.Globalization;
using EmberStart.Core.Geometry;

namespace EmberStart.Core.Activation;

public static class ActivationCommandParser
{
    private const int IntegratedArgumentCount = 13;

    public static ActivationParseResult Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 0)
        {
            return ActivationParseResult.Accepted(
                ActivationRequest.CreateSimple(ActivationCommand.Show, ActivationSource.CommandLine));
        }

        if (arguments.Count == 1)
        {
            return ParseSimple(arguments[0]);
        }

        return ParseIntegrated(arguments);
    }

    private static ActivationParseResult ParseSimple(string argument)
    {
        var command = argument switch
        {
            "--toggle" => ActivationCommand.Toggle,
            "--show" => ActivationCommand.Show,
            "--hide" => ActivationCommand.Hide,
            _ => (ActivationCommand?)null,
        };

        return command is { } accepted
            ? ActivationParseResult.Accepted(ActivationRequest.CreateSimple(accepted, ActivationSource.CommandLine))
            : ActivationParseResult.Rejected("Expected --toggle, --show, or --hide.");
    }

    private static ActivationParseResult ParseIntegrated(IReadOnlyList<string> arguments)
    {
        if (arguments.Count != IntegratedArgumentCount ||
            arguments[0] != "--integrated-toggle-v1" ||
            arguments[1] != "--source" || arguments[2] != "retrobar" ||
            arguments[3] != "--anchor-left" ||
            arguments[5] != "--anchor-top" ||
            arguments[7] != "--anchor-right" ||
            arguments[9] != "--anchor-bottom" ||
            arguments[11] != "--taskbar-edge")
        {
            return ActivationParseResult.Rejected("The integrated activation form is malformed.");
        }

        if (!TryParseInt32(arguments[4], out var left) ||
            !TryParseInt32(arguments[6], out var top) ||
            !TryParseInt32(arguments[8], out var right) ||
            !TryParseInt32(arguments[10], out var bottom))
        {
            return ActivationParseResult.Rejected("Anchor coordinates must be signed 32-bit integers.");
        }

        if (!Enum.TryParse<TaskbarEdge>(arguments[12], ignoreCase: true, out var edge))
        {
            return ActivationParseResult.Rejected("Taskbar edge must be left, top, right, or bottom.");
        }

        var anchor = new PhysicalRect(left, top, right, bottom);
        if (!anchor.IsOrdered)
        {
            return ActivationParseResult.Rejected("Anchor bounds must have positive width and height.");
        }

        var request = new ActivationRequest(
            ActivationRequest.CurrentProtocolVersion,
            Guid.NewGuid(),
            ActivationCommand.Toggle,
            ActivationSource.RetroBar,
            anchor,
            edge);

        return ActivationParseResult.Accepted(request);
    }

    private static bool TryParseInt32(string value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
}
