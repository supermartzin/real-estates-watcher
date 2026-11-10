using RealEstatesWatcher.UI.Console;

namespace RealEstatesWatcher.Tests;

public class CmdArgumentsTests
{
    [Theory]
    [InlineData("--config")]
    [InlineData("-c")]
    public async Task Parse_AcceptsSingleConfigArgument(string option)
    {
        var arguments = new CmdArguments();

        Assert.True(await arguments.ParseAsync([option, "configs/watcher.ini"]));
        Assert.Equal("configs/watcher.ini", arguments.ConfigFilePath);
    }

    [Theory]
    [InlineData()]
    [InlineData("--config")]
    [InlineData("--config", "watcher.ini", "--unknown", "value")]
    [InlineData("--e", "engine.ini", "--h", "handlers.ini", "--p", "portals.ini")]
    public async Task Parse_RejectsMissingOrUnsupportedArguments(params string[] values)
    {
        Assert.False(await new CmdArguments().ParseAsync(values));
    }

    [Fact]
    public async Task Parse_ReturnsFalseForHelp()
    {
        Assert.False(await new CmdArguments().ParseAsync(["--help"]));
    }

    [Fact]
    public async Task Parse_RejectsNullArguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new CmdArguments().ParseAsync(null!));
    }
}
