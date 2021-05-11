using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

using RealEstatesWatcher.Core;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.UI.Console
{
    public class ConsoleRunner
    {
        private static readonly AutoResetEvent WaitHandle = new(false);

        private static IServiceProvider _container;
        private static ILogger<ConsoleRunner>? _logger;

        public static async Task Main(string[] args)
        {
            ConfigureDependencyInjection();

            _logger = _container.GetService<ILogger<ConsoleRunner>>();

            try
            {
                var watcher = _container.GetRequiredService<RealEstatesWatchEngine>();
                RegisterAdsPortals(watcher);
                RegisterAdPostsHandlers(watcher);

                // start watcher
                await watcher.StartAsync();

                WaitForExitSignal();
            }
            catch (RealEstatesWatchEngineException reweEx)
            {
                _logger?.LogCritical(reweEx, $"Error starting Real estates Watcher: {reweEx.Message}");
            }
            finally
            {
                System.Console.ReadKey();
            }
        }
        
        private static void ConfigureDependencyInjection()
        {
            var collection = new ServiceCollection();

            // add logging
            collection.AddLogging(builder =>
            {
                builder.AddNLog();

                // set Minimum log level based on variable in NLog.config --> default == INFO
                var minLevelVariable = LogManager.Configuration?.Variables["minLogLevel"]?.OriginalText;
                if (minLevelVariable is not null && Enum.TryParse(minLevelVariable, true, out LogLevel minLevel))
                    builder.SetMinimumLevel(minLevel);
            });

            // add scraper
            collection.AddSingleton<IWebScraper>(new LocalNodejsConsoleWebScraper("./scraper/index.js"));
            
            // add engine
            collection.AddSingleton(new WatchEngineSettings
            {
                CheckIntervalMinutes = 1
            });
            collection.AddSingleton<RealEstatesWatchEngine>();

            _container = collection.BuildServiceProvider();
        }

        private static void RegisterAdsPortals(RealEstatesWatchEngine watcher)
        {

        }

        private static void RegisterAdPostsHandlers(RealEstatesWatchEngine watcher)
        {

        }

        private static void WaitForExitSignal()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var input = System.Console.ReadLine();
                    if (input != "exit")
                        continue;

                    WaitHandle.Set();
                    break;
                }
            });

            WaitHandle.WaitOne();
        }
    }
}
