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
    private static readonly AutoResetEvent WaitHandle = new(false);
    private static readonly CmdArguments CmdArguments = new();

    private static IServiceProvider? _container;
    private static ILogger<ConsoleRunner>? _logger;
    
    protected ConsoleRunner() { }

    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var parsed = await CmdArguments.ParseAsync(args);
        if (!parsed)
            return;

        ConfigureDependencyInjection();
        if (_container is null)
        {
            _logger?.LogCritical("Error starting Real estates Watcher: Unable to build all required components.");
            return;
        }

        _logger = _container.GetService<ILogger<ConsoleRunner>>();
        _logger?.LogInformation("------------------------------------------------");

        try
        {
            var watcher = _container.GetRequiredService<RealEstatesWatchEngine>();
            RegisterAdsPortals(watcher, _container);
            RegisterAdPostsHandlers(watcher, _container);
            RegisterAdPostsFilters(watcher, _container);

            _logger?.LogInformation("Starting Real estate Watcher engine...");

            // start watcher
            await watcher.StartAsync().ConfigureAwait(false);

            WaitForExitSignal();

            // stop watcher
            await watcher.StopAsync().ConfigureAwait(false);
        }
        catch (RealEstatesWatchEngineException reweEx)
        {
            _logger?.LogCritical(reweEx, "Error starting Real estates Watcher: {Message}", reweEx.Message);
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
                
                options.AutoSessionTracking = true;
                options.TracesSampleRate = 1.0;
                options.ProfilesSampleRate = 1.0;
                options.AddIntegration(new ProfilingIntegration());
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
            CheckIntervalMinutes = configuration.GetValue<int>("check_interval_minutes"),
            EnableMultiplePortalInstances = configuration.GetValue<bool>("enable_multiple_portal_instances")
        };
    }

    private static void RegisterAdsPortals(RealEstatesWatchEngine watcher, IServiceProvider container)
    {
        _logger?.LogInformation("Registering Ads portals..");

        if (!File.Exists(CmdArguments.PortalsConfigFilePath))
            throw new ArgumentOutOfRangeException($"Configuration file for Ads portals not found at '{CmdArguments.PortalsConfigFilePath}'!");

        var urls = File.ReadAllLines(CmdArguments.PortalsConfigFilePath);

        foreach (var url in urls)
        {
            if (url.StartsWith("//"))
            {
                // comment
                continue; 
            }

            RegisterAdsPortalInstanceByUrl(watcher, container, url);
        }
    }

    private static void RegisterAdsPortalInstanceByUrl(RealEstatesWatchEngine watcher, IServiceProvider container, string url)
    {
        if (url.Contains("bazos.cz"))
        {
            watcher.RegisterAdsPortal(new BazosCzAdsPortal(url, container.GetService<ILogger<BazosCzAdsPortal>>()));
            return;
        }
        if (url.Contains("bezrealitky.cz"))
        {
            watcher.RegisterAdsPortal(new BezrealitkyCzAdsPortal(url,
                container.GetRequiredService<IWebScraper>(),
                container.GetService<ILogger<BezrealitkyCzAdsPortal>>()));
            return;
        }
        if (url.Contains("bidli.cz"))
        {
            watcher.RegisterAdsPortal(new BidliCzAdsPortal(url, container.GetService<ILogger<BidliCzAdsPortal>>()));
            return;
        }
        if (url.Contains("bravis.cz"))
        {
            watcher.RegisterAdsPortal(new BravisCzAdsPortal(url, container.GetService<ILogger<BravisCzAdsPortal>>()));
            return;
        }
        if (url.Contains("ceskereality.cz"))
        {
            watcher.RegisterAdsPortal(new CeskeRealityCzAdsPortal(url, container.GetService<ILogger<CeskeRealityCzAdsPortal>>()));
            return;
        }
        if (url.Contains("flatzone.cz"))
        {
            watcher.RegisterAdsPortal(new FlatZoneCzAdsPortal(url,
                container.GetRequiredService<IWebScraper>(),
                container.GetService<ILogger<FlatZoneCzAdsPortal>>()));
            return;
        }
        if (url.Contains("mmreality.cz"))
        {
            watcher.RegisterAdsPortal(new MmRealityCzAdsPortal(url, container.GetService<ILogger<MmRealityCzAdsPortal>>()));
            return;
        }
        if (url.Contains("realcity.cz"))
        {
            watcher.RegisterAdsPortal(new RealcityCzAdsPortal(url, container.GetService<ILogger<RealcityCzAdsPortal>>()));
            return;
        }
        if (url.Contains("reality.idnes.cz"))
        {
            watcher.RegisterAdsPortal(new RealityIdnesCzAdsPortal(url, container.GetService<ILogger<RealityIdnesCzAdsPortal>>()));
            return;
        }
        if (url.Contains("remax.cz"))
        {
            watcher.RegisterAdsPortal(new RemaxCzAdsProtal(url, container.GetService<ILogger<RemaxCzAdsProtal>>()));
            return;
        }
        if (url.Contains("sreality.cz"))
        {
            watcher.RegisterAdsPortal(new SrealityCzAdsPortal(url,
                container.GetRequiredService<IWebScraper>(),
                container.GetService<ILogger<SrealityCzAdsPortal>>()));
        }
    }

    private static void RegisterAdPostsHandlers(RealEstatesWatchEngine watcher, IServiceProvider container)
    {
        _logger?.LogInformation("Registering Ad posts handlers..");

        watcher.RegisterAdPostsHandler(new EmailNotifyingAdPostsHandler(LoadEmailSettings(), container.GetService<ILogger<EmailNotifyingAdPostsHandler>>()));
        watcher.RegisterAdPostsHandler(new LocalFileAdPostsHandler(LoadFileSettings()));

        static EmailNotifyingAdPostsHandlerSettings LoadEmailSettings()
        {
            var configuration = new ConfigurationBuilder().AddIniFile(CmdArguments.HandlersConfigFilePath)
                .Build()
                .GetSection("email");

            return new EmailNotifyingAdPostsHandlerSettings
            {
                Enabled = configuration.GetValue<bool>("enabled"),
                FromAddress = configuration["from"],
                ToAddresses = string.IsNullOrEmpty(configuration["to"]) ? [] : configuration["to"]?.Split(',') ?? [],
                CcAddresses = string.IsNullOrEmpty(configuration["cc"]) ? [] : configuration["cc"]?.Split(',') ?? [],
                BccAddresses = string.IsNullOrEmpty(configuration["bcc"]) ? [] : configuration["bcc"]?.Split(',') ?? [],
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

    private static void RegisterAdPostsFilters(RealEstatesWatchEngine watcher, IServiceProvider container)
    {
        _logger?.LogInformation("Registering Ad posts filters..");

        if (string.IsNullOrEmpty(CmdArguments.FiltersConfigFilePath))
        {
            _logger?.LogInformation("No filters provided.");
            return;
        }

        watcher.RegisterAdPostsFilter(new BasicParametersAdPostsFilter(LoadBasicFilterSettings(CmdArguments.FiltersConfigFilePath), container.GetService<ILogger<BasicParametersAdPostsFilter>>()));
        
        static BasicParametersAdPostsFilterSettings LoadBasicFilterSettings(string filtersConfigFilePath)
        {
            var configuration = new ConfigurationBuilder().AddIniFile(filtersConfigFilePath)
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

            var layouts = layoutsValue.Split(",")
                                      .Select(LayoutExtensions.ToLayout)
                                      .ToHashSet();
            
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