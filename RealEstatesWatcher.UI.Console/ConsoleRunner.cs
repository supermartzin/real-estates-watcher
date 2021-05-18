using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

using RealEstatesWatcher.AdsPortals.BazosCz;
using RealEstatesWatcher.AdsPortals.FlatZoneCz;
using RealEstatesWatcher.AdsPortals.RemaxCz;
using RealEstatesWatcher.AdsPortals.SrealityCz;
using RealEstatesWatcher.AdPostsHandlers.Email;
using RealEstatesWatcher.AdsPortals.BezrealitkyCz;
using RealEstatesWatcher.AdsPortals.CeskeRealityCz;
using RealEstatesWatcher.AdsPortals.MMRealityCz;
using RealEstatesWatcher.AdsPortals.RealcityCz;
using RealEstatesWatcher.AdsPortals.RealityIdnesCz;
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

                // stop watcher
                await watcher.StopAsync();
            }
            catch (RealEstatesWatchEngineException reweEx)
            {
                _logger?.LogCritical(reweEx, $"Error starting Real estates Watcher: {reweEx.Message}");
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
            collection.AddSingleton(LoadWatchEngineSettings());
            collection.AddSingleton<RealEstatesWatchEngine>();

            _container = collection.BuildServiceProvider();
        }

        private static WatchEngineSettings LoadWatchEngineSettings()
        {
            var configuration = new ConfigurationBuilder().AddIniFile("./configs/engine.ini")
                                                          .Build()
                                                          .GetSection("settings");

            return new WatchEngineSettings
            {
                CheckIntervalMinutes = configuration.GetValue<int>("check_interval_minutes")
            };
        }

        private static void RegisterAdsPortals(RealEstatesWatchEngine watcher)
        {
            _logger?.LogInformation("Registering Ads portals..");

            var configuration = new ConfigurationBuilder().AddIniFile("./configs/portals.ini").Build();

            foreach (var section in configuration.GetChildren())
            {
                var url = section["url"];

                switch (section.Key)
                {
                    case "Bazos.cz":
                        watcher.RegisterAdsPortal(new BazosCzAdsPortal(url, _container.GetService<ILogger<BazosCzAdsPortal>>()));
                        break;

                    case "Bezrealitky.cz":
                        watcher.RegisterAdsPortal(new BezrealitkyCzAdsPortal(url,
                                                                             _container.GetRequiredService<IWebScraper>(),
                                                                             _container.GetService<ILogger<BezrealitkyCzAdsPortal>>()));
                        break;

                    case "Ceskereality.cz":
                        watcher.RegisterAdsPortal(new CeskeRealityCzAdsPortal(url, _container.GetService<ILogger<CeskeRealityCzAdsPortal>>()));
                        break;

                    case "FlatZone.cz":
                        watcher.RegisterAdsPortal(new FlatZoneCzAdsPortal(url,
                                                                          _container.GetRequiredService<IWebScraper>(),
                                                                          _container.GetService<ILogger<FlatZoneCzAdsPortal>>()));
                        break;

                    case "MMReality.cz":
                        watcher.RegisterAdsPortal(new MMRealityCzAdsPortal(url, _container.GetService<ILogger<MMRealityCzAdsPortal>>()));
                        break;

                    case "Realcity.cz":
                        watcher.RegisterAdsPortal(new RealcityCzAdsPortal(url, _container.GetService<ILogger<RealcityCzAdsPortal>>()));
                        break;

                    case "Reality.idnes.cz":
                        watcher.RegisterAdsPortal(new RealityIdnesCzAdsPortal(url, _container.GetService<ILogger<RealityIdnesCzAdsPortal>>()));
                        break;

                    case "Remax.cz":
                        watcher.RegisterAdsPortal(new RemaxCzAdsProtal(url, _container.GetService<ILogger<RemaxCzAdsProtal>>()));
                        break;

                    case "Sreality.cz":
                        watcher.RegisterAdsPortal(new SrealityCzAdsPortal(url,
                                                                          _container.GetRequiredService<IWebScraper>(),
                                                                          _container.GetService<ILogger<SrealityCzAdsPortal>>()));
                        break;
                }
            }
        }

        private static void RegisterAdPostsHandlers(RealEstatesWatchEngine watcher)
        {
            _logger?.LogInformation("Registering Ad posts handlers..");

            //watcher.RegisterAdPostsHandler(new EmailNotifyingAdPostsHandler(LoadSettings(), _container.GetService<ILogger<EmailNotifyingAdPostsHandler>>()));
            watcher.RegisterAdPostsHandler(new FileAdPostsHandler());

            static EmailNotifyingAdPostsHandlerSettings LoadSettings()
            {
                var configuration = new ConfigurationBuilder().AddIniFile("./configs/handlers.ini")
                                                              .Build()
                                                              .GetSection("email");

                return new EmailNotifyingAdPostsHandlerSettings
                {
                    EmailAddressFrom = configuration["email_address_from"],
                    EmailAddressesTo = configuration["email_addresses_to"].Split(','),
                    SenderName = configuration["sender_name"],
                    SmtpServerHost = configuration["smtp_server_host"],
                    SmtpServerPort = configuration.GetValue<int>("smtp_server_port"),
                    UseSecureConnection = configuration.GetValue<bool>("use_secure_connection"),
                    Username = configuration["username"],
                    Password = configuration["password"],
                    SkipInitialNotification = configuration.GetValue<bool>("skip_initial_notification")
                };
            }
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
