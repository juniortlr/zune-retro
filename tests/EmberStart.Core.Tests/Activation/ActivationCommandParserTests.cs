using EmberStart.Core.Activation;

namespace EmberStart.Core.Tests.Activation;

public sealed class ActivationCommandParserTests
{
    [Theory]
    [InlineData("--toggle", ActivationCommand.Toggle)]
    [InlineData("--show", ActivationCommand.Show)]
    [InlineData("--hide", ActivationCommand.Hide)]
    public void Parse_AcceptsSimpleCommands(string argument, ActivationCommand expected)
    {
        var result = ActivationCommandParser.Parse([argument]);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Request!.Command);
        Assert.Equal(ActivationSource.CommandLine, result.Request.Source);
    }

    [Fact]
    public void Parse_NoArguments_ShowsMenuForDirectLaunch()
    {
        var result = ActivationCommandParser.Parse([]);

        Assert.True(result.Success);
        Assert.Equal(ActivationCommand.Show, result.Request!.Command);
    }

    [Fact]
    public void Parse_AcceptsStrictIntegratedForm()
    {
        string[] arguments =
        [
            "--integrated-toggle-v1", "--source", "retrobar",
            "--anchor-left", "-1920", "--anchor-top", "1040",
            "--anchor-right", "-1840", "--anchor-bottom", "1080",
            "--taskbar-edge", "bottom",
        ];

        var result = ActivationCommandParser.Parse(arguments);

        Assert.True(result.Success);
        Assert.Equal(ActivationSource.RetroBar, result.Request!.Source);
        Assert.Equal(TaskbarEdge.Bottom, result.Request.Edge);
        Assert.Equal(-1920, result.Request.Anchor!.Value.Left);
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--toggle --extra")]
    public void Parse_RejectsUnknownOrExtraArguments(string commandLine)
    {
        var result = ActivationCommandParser.Parse(commandLine.Split(' '));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Parse_RejectsUnorderedAnchor()
    {
        string[] arguments =
        [
            "--integrated-toggle-v1", "--source", "retrobar",
            "--anchor-left", "100", "--anchor-top", "100",
            "--anchor-right", "20", "--anchor-bottom", "120",
            "--taskbar-edge", "bottom",
        ];

        var result = ActivationCommandParser.Parse(arguments);

        Assert.False(result.Success);
        Assert.Contains("positive", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
