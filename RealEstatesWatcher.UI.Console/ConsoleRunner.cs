using System.Text;
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
using RealEstatesWatcher.AdsPortals.BezrealitkyCz;
using RealEstatesWatcher.AdsPortals.BidliCz;
using RealEstatesWatcher.AdsPortals.BravisCz;
using RealEstatesWatcher.AdsPortals.CeskeRealityCz;
using RealEstatesWatcher.AdsPortals.MMRealityCz;
using RealEstatesWatcher.AdsPortals.RealcityCz;
using RealEstatesWatcher.AdsPortals.RealityIdnesCz;
using RealEstatesWatcher.Core;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Email;
using RealEstatesWatcher.AdPostsHandlers.File;
using RealEstatesWatcher.AdPostsFilters.BasicFilter;

namespace RealEstatesWatcher.UI.Console;

public class ConsoleRunner
{
    private static readonly AutoResetEvent WaitHandle;
    private static readonly CmdArguments CmdArguments;

    private static IServiceProvider _container;
    private static ILogger<ConsoleRunner>? _logger;

    static ConsoleRunner()
    {
        WaitHandle = new AutoResetEvent(false);
        CmdArguments = new CmdArguments();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static async Task Main(string[] args)
    {
        var parsed = await CmdArguments.ParseAsync(args);
        if (!parsed)
            return;

        ConfigureDependencyInjection();

        _logger = _container.GetService<ILogger<ConsoleRunner>>();

        try
        {
            var watcher = _container.GetRequiredService<RealEstatesWatchEngine>();
            RegisterAdsPortals(watcher);
            RegisterAdPostsHandlers(watcher);
            RegisterAdPostsFilters(watcher);

            _logger?.LogInformation("Starting Real estate Watcher engine...");

            // start watcher
            await watcher.StartAsync().ConfigureAwait(false);

            WaitForExitSignal();

            // stop watcher
            await watcher.StopAsync().ConfigureAwait(false);
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
            builder.AddSentry(options =>
            {
                options.Dsn = "https://8a560ec7c9c241c6bc9f00e116bace08@o504575.ingest.sentry.io/5792424";
                options.MinimumEventLevel = LogLevel.Warning;
                options.InitializeSdk = true;
            });

            // set Minimum log level based on variable in NLog.config --> default == INFO
            var minLevelVariable = LogManager.Configuration?.Variables["minLogLevel"].ToString();
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
        var configuration = new ConfigurationBuilder()
            .AddIniFile(CmdArguments.EngineConfigFilePath)
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

        var configuration = new ConfigurationBuilder().AddIniFile(CmdArguments.PortalsConfigFilePath).Build();

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

                case "Bidli.cz":
                    watcher.RegisterAdsPortal(new BidliCzAdsPortal(url, _container.GetService<ILogger<BidliCzAdsPortal>>()));
                    break;

                case "Bravis.cz":
                    watcher.RegisterAdsPortal(new BravisCzAdsPortal(url, _container.GetService<ILogger<BravisCzAdsPortal>>()));
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

        watcher.RegisterAdPostsHandler(new EmailNotifyingAdPostsHandler(LoadEmailSettings(), _container.GetService<ILogger<EmailNotifyingAdPostsHandler>>()));
        watcher.RegisterAdPostsHandler(new LocalFileAdPostsHandler(LoadFileSettings()));

        static EmailNotifyingAdPostsHandlerSettings LoadEmailSettings()
        {
            var configuration = new ConfigurationBuilder().AddIniFile(CmdArguments.HandlersConfigFilePath)
                .Build()
                .GetSection("email");

            return new EmailNotifyingAdPostsHandlerSettings
            {
                Enabled = configuration.GetValue<bool>("enabled"),
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

        static LocalFileAdPostsHandlerSettings LoadFileSettings()
        {
            var configuration = new ConfigurationBuilder().AddIniFile(CmdArguments.HandlersConfigFilePath)
                .Build()
                .GetSection("file");

            return new LocalFileAdPostsHandlerSettings
            {
                Enabled = configuration.GetValue<bool>("enabled"),
                MainFilePath = configuration["main_path"],
                NewPostsToSeparateFile = configuration.GetValue<bool>("separate_new_posts"),
                NewPostsFilePath = configuration["new_posts_path"]
            };
        }
    }

    private static void RegisterAdPostsFilters(RealEstatesWatchEngine watcher)
    {
        _logger?.LogInformation("Registering Ad posts filters..");

        if (CmdArguments.FiltersConfigFilePath is null)
        {
            _logger?.LogInformation("No filters provided.");
            return;
        }

        watcher.RegisterAdPostsFilter(new BasicParametersAdPostsFilter(LoadBasicFilterSettings(), _container.GetService<ILogger<BasicParametersAdPostsFilter>>()));

        static BasicParametersAdPostsFilterSettings LoadBasicFilterSettings()
        {
            var configuration = new ConfigurationBuilder().AddIniFile(CmdArguments.FiltersConfigFilePath)
                .Build()
                .GetSection("basic");

            return new BasicParametersAdPostsFilterSettings
            {
                MinPrice = configuration.GetValue<decimal?>("price_min"),
                MaxPrice = configuration.GetValue<decimal?>("price_max"),
                MinFloorArea = configuration.GetValue<decimal?>("floor_area_min"),
                MaxFloorArea = configuration.GetValue<decimal?>("floor_area_max"),
                Layouts = ParseLayouts(configuration.GetValue<string?>("layouts"))
            };
        }

        static ISet<Layout> ParseLayouts(string? layoutsValue)
        {
            if (string.IsNullOrWhiteSpace(layoutsValue))
                return new HashSet<Layout>();

            var layouts = layoutsValue.Split(",").Select(LayoutExtensions.ToLayout).ToHashSet();
            if (layouts.Contains(Layout.NotSpecified))
                layouts.Remove(Layout.NotSpecified);

            return layouts;
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