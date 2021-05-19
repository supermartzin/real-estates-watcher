using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace RealEstatesWatcher.UI.Console
{
    public class CmdArguments
    {
        private readonly RootCommand _rootCommand;

        public string PortalsConfigFilePath { get; private set; }
        
        public string HandlersConfigFilePath { get; private set; }

        public string EngineConfigFilePath { get; private set; }

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
                new Option<string>(new[] {"-engine", "--e"}, "The path to the configuration file of the watcher engine")
                {
                    ArgumentHelpName = "path to file",
                    IsRequired = true
                }
            };
            _rootCommand.Description =
                "Script for real-time periodic watching of Real estate advertisement portals with notifications on new ads.";
        }

        public async Task ParseAsync(string[] arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            _rootCommand.Handler = CommandHandler.Create<string, string, string>(
                (portals, handlers, engine) =>
                {
                    PortalsConfigFilePath = portals;
                    HandlersConfigFilePath = handlers;
                    EngineConfigFilePath = engine;
                });

            await _rootCommand.InvokeAsync(arguments).ConfigureAwait(false);
        }
    }
}