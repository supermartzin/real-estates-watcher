using System.CommandLine;
using System.CommandLine.Invocation;

namespace RealEstatesWatcher.UI.Console;

public class CmdArguments
{
    private readonly RootCommand _rootCommand;

    public string? PortalsConfigFilePath { get; private set; }
        
    public string? HandlersConfigFilePath { get; private set; }

    public string? EngineConfigFilePath { get; private set; }

    public string? FiltersConfigFilePath { get; private set; }

    public CmdArguments()
    {
        _rootCommand = new RootCommand
        {
            new Option<string>(new[] {"-portals", "--p"}, "The path to the configuration file of supported Ads portals")
            {
                ArgumentHelpName = "path to file",
                IsRequired = true
            },
            new Option<string>(new[] {"-handlers", "--h"}, "The path to the configuration file of Ad posts Handlers")
            {
                ArgumentHelpName = "path to file",
                IsRequired = true
            },
            new Option<string?>(new [] {"-filters", "--f"}, "The path to the configuration file of Ad posts filters")
            {
                ArgumentHelpName = "path to file",
                IsRequired = false
            },
            new Option<string>(new[] {"-engine", "--e"}, "The path to the configuration file of the watcher engine")
            {
                ArgumentHelpName = "path to file",
                IsRequired = true
            }
        };
        _rootCommand.Description =
            "Script for real-time periodic watching of Real estate advertisement portals with notifications on new ads.";
    }

    public async Task<bool> ParseAsync(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var parsed = false;

        _rootCommand.Handler = CommandHandler.Create<string?, string?, string?, string?>(
            (portals, handlers, filters, engine) =>
            {
                PortalsConfigFilePath = portals;
                HandlersConfigFilePath = handlers;
                EngineConfigFilePath = engine;
                FiltersConfigFilePath = filters;

                parsed = true;
            });

        await _rootCommand.InvokeAsync(arguments).ConfigureAwait(false);

        return parsed;
    }
}