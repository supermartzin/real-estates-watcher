using RealEstatesWatcher.UI.Console;

namespace RealEstatesWatcher.Tests;

public class CmdArgumentsTests
{
    [Fact]
    public async Task Parse_MapsRequiredAndOptionalArguments()
    {
        var arguments = new CmdArguments();

        var parsed = await arguments.ParseAsync(
        [
            "-portals", "portals.ini",
            "-handlers", "handlers.ini",
            "-engine", "engine.ini",
            "-filters", "filters.ini",
            "-scraper", "scraper.ini"
        ]);

        Assert.True(parsed);
        Assert.Equal("portals.ini", arguments.PortalsConfigFilePath);
        Assert.Equal("handlers.ini", arguments.HandlersConfigFilePath);
        Assert.Equal("engine.ini", arguments.EngineConfigFilePath);
        Assert.Equal("filters.ini", arguments.FiltersConfigFilePath);
        Assert.Equal("scraper.ini", arguments.WebScraperConfigFilePath);
    }

    [Fact]
    public async Task Parse_AllowsOptionalArgumentsToBeOmitted()
    {
        var arguments = new CmdArguments();

        var parsed = await arguments.ParseAsync(
        [
            "-portals", "portals.ini",
            "-handlers", "handlers.ini",
            "-engine", "engine.ini"
        ]);

        Assert.True(parsed);
        Assert.Null(arguments.FiltersConfigFilePath);
        Assert.Null(arguments.WebScraperConfigFilePath);
    }

    [Fact]
    public async Task Parse_ReturnsFalseWhenRequiredArgumentsAreMissing()
    {
        var arguments = new CmdArguments();

        Assert.False(await arguments.ParseAsync([]));
    }

    [Fact]
    public async Task Parse_AcceptsConfiguredAliases()
    {
        var arguments = new CmdArguments();

        var parsed = await arguments.ParseAsync(
        [
            "--p", "portals.ini",
            "--h", "handlers.ini",
            "--e", "engine.ini",
            "--f", "filters.ini",
            "--s", "scraper.ini"
        ]);

        Assert.True(parsed);
        Assert.Equal("portals.ini", arguments.PortalsConfigFilePath);
        Assert.Equal("scraper.ini", arguments.WebScraperConfigFilePath);
    }

    [Fact]
    public async Task Parse_ReturnsFalseForUnknownOptions()
    {
        var arguments = new CmdArguments();

        var parsed = await arguments.ParseAsync(
        [
            "-portals", "portals.ini",
            "-handlers", "handlers.ini",
            "-engine", "engine.ini",
            "--unknown", "value"
        ]);

        Assert.False(parsed);
    }

    [Fact]
    public async Task Parse_RejectsNullArguments()
    {
        var arguments = new CmdArguments();

        await Assert.ThrowsAsync<ArgumentNullException>(() => arguments.ParseAsync(null!));
    }
}
