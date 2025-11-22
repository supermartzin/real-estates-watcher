using System.CommandLine;

namespace RealEstatesWatcher.UI.Console;

public class CmdArguments
{
    private readonly RootCommand _rootCommand;

    private readonly Option<string> _portalsFileOption;
    private readonly Option<string> _handlersFileOption;
    private readonly Option<string> _engineConfigurationFileOption;
    private readonly Option<string?> _filtersFileOption;
    private readonly Option<string?> _scraperFileOption;
    private readonly Option<string?> _applicationIdOption;

    public string PortalsConfigFilePath { get; private set; } = string.Empty;

    public string HandlersConfigFilePath { get; private set; } = string.Empty;

    public string EngineConfigFilePath { get; private set; } = string.Empty;

    public string? FiltersConfigFilePath { get; private set; }

    public string? WebScraperConfigFilePath { get; private set; }

    public string? ApplicationId { get; private set; }

    public CmdArguments()
    {
        _portalsFileOption = new Option<string>("-portals", "--p")
        {
            Description = "The path to the configuration file of supported Ads portals",
            Required = true
        };
        _handlersFileOption = new Option<string>("-handlers", "--h")
        {
            Description = "The path to the configuration file of Ad posts Handlers",
            Required = true
        };
        _engineConfigurationFileOption = new Option<string>("-engine", "--e")
        {
            Description = "The path to the configuration file of the watcher engine",
            Required = true
        };
        _filtersFileOption = new Option<string?>("-filters", "--f")
        {
            Description = "The path to the configuration file of Ad posts filters",
            Required = false
        };
        _scraperFileOption = new Option<string?>("-scraper", "--s")
        {
            Description = "The path to the configuration file of the web scraper",
            Required = false
        };
        _applicationIdOption = new Option<string?>("-application_id", "--id")
        {
            Description = "The application ID for identifying specific instance of the app",
            Required = false
        };

        _rootCommand =
        [
            _portalsFileOption,
            _handlersFileOption,
            _engineConfigurationFileOption,
            _filtersFileOption,
            _scraperFileOption,
            _applicationIdOption
        ];
        _rootCommand.Description =
            "Script for real-time periodic watching of Real estate advertisement portals with notifications on new ads.";
    }

    public async Task<bool> ParseAsync(string[] arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var parsed = false;

        _rootCommand.SetAction(parsedResults =>
        {
            PortalsConfigFilePath = parsedResults.GetRequiredValue(_portalsFileOption);
            HandlersConfigFilePath = parsedResults.GetRequiredValue(_handlersFileOption);
            EngineConfigFilePath = parsedResults.GetRequiredValue(_engineConfigurationFileOption);
            FiltersConfigFilePath = parsedResults.GetValue(_filtersFileOption);
            WebScraperConfigFilePath = parsedResults.GetValue(_scraperFileOption);
            ApplicationId = parsedResults.GetValue(_applicationIdOption);

            parsed = true;
        });

        await _rootCommand.Parse(arguments).InvokeAsync(cancellationToken: cancellationToken);
        
        return parsed;
    }
}