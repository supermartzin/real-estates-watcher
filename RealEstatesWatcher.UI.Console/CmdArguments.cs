using System.CommandLine;

namespace RealEstatesWatcher.UI.Console;

public class CmdArguments
{
    private const string DefaultArgumentHelpName = "path to file";

    private readonly RootCommand _rootCommand;

    private readonly Option<string> _portalsFileOption;
    private readonly Option<string> _handlersFileOption;
    private readonly Option<string> _engineConfigurationFileOption;
    private readonly Option<string?> _filtersFileOption;
    private readonly Option<string?> _scraperFileOption;

    public string PortalsConfigFilePath { get; private set; } = string.Empty;

    public string HandlersConfigFilePath { get; private set; } = string.Empty;

    public string EngineConfigFilePath { get; private set; } = string.Empty;

    public string? FiltersConfigFilePath { get; private set; }

    public string? WebScraperConfigFilePath { get; private set; }

    public CmdArguments()
    {
        _portalsFileOption = new Option<string>(["-portals", "--p"], "The path to the configuration file of supported Ads portals")
        {
            ArgumentHelpName = DefaultArgumentHelpName,
            IsRequired = true
        };
        _handlersFileOption = new Option<string>(["-handlers", "--h"], "The path to the configuration file of Ad posts Handlers")
        {
            ArgumentHelpName = DefaultArgumentHelpName,
            IsRequired = true
        };
        _engineConfigurationFileOption = new Option<string>(["-engine", "--e"], "The path to the configuration file of the watcher engine")
        {
            ArgumentHelpName = DefaultArgumentHelpName,
            IsRequired = true
        };
        _filtersFileOption = new Option<string?>(["-filters", "--f"], "The path to the configuration file of Ad posts filters")
        {
            ArgumentHelpName = DefaultArgumentHelpName,
            IsRequired = false
        };
        _scraperFileOption = new Option<string?>(["-scraper", "--s"], "The path to the configuration file of the web scraper")
        {
            ArgumentHelpName = DefaultArgumentHelpName,
            IsRequired = false
        };

        _rootCommand =
        [
            _portalsFileOption,
            _handlersFileOption,
            _engineConfigurationFileOption,
            _filtersFileOption,
            _scraperFileOption
        ];
        _rootCommand.Description =
            "Script for real-time periodic watching of Real estate advertisement portals with notifications on new ads.";
    }

    public async Task<bool> ParseAsync(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var parsed = false;
        
        _rootCommand.SetHandler((portals, handlers, engine, filters, scraper) =>
        {
            PortalsConfigFilePath = portals;
            HandlersConfigFilePath = handlers;
            EngineConfigFilePath = engine;
            FiltersConfigFilePath = filters;
            WebScraperConfigFilePath = scraper;

            parsed = true;
        },  _portalsFileOption, _handlersFileOption, _engineConfigurationFileOption, _filtersFileOption, _scraperFileOption);

        await _rootCommand.InvokeAsync(arguments).ConfigureAwait(false);

        return parsed;
    }
}