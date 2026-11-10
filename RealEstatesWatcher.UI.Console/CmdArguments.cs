using System.CommandLine;

namespace RealEstatesWatcher.UI.Console;

public class CmdArguments
{
    private readonly RootCommand _rootCommand;
    private readonly Option<string> _configFileOption;

    public string ConfigFilePath { get; private set; } = string.Empty;

    public CmdArguments()
    {
        _configFileOption = new Option<string>("--config", "-c")
        {
            Description = "The path to the watcher INI configuration file",
            Required = true
        };

        _rootCommand = [_configFileOption];
        _rootCommand.Description =
            "Script for real-time periodic watching of Real estate advertisement portals with notifications on new ads.";
    }

    public async Task<bool> ParseAsync(string[] arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var parsed = false;
        _rootCommand.SetAction(parsedResults =>
        {
            ConfigFilePath = parsedResults.GetRequiredValue(_configFileOption);
            parsed = true;
        });

        await _rootCommand.Parse(arguments).InvokeAsync(cancellationToken: cancellationToken);
        return parsed;
    }
}
