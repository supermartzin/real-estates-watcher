using Google.Cloud.Diagnostics.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;

using RealEstatesWatcher.AdPostsFilters.BasicFilter;
using RealEstatesWatcher.AdPostsHandlers.Email;
using RealEstatesWatcher.AdPostsHandlers.File;
using RealEstatesWatcher.AdsPortals.BazosCz;
using RealEstatesWatcher.AdsPortals.BezrealitkyCz;
using RealEstatesWatcher.AdsPortals.BidliCz;
using RealEstatesWatcher.AdsPortals.BravisCz;
using RealEstatesWatcher.AdsPortals.CeskeRealityCz;
using RealEstatesWatcher.AdsPortals.FlatZoneCz;
using RealEstatesWatcher.AdsPortals.MMRealityCz;
using RealEstatesWatcher.AdsPortals.RealcityCz;
using RealEstatesWatcher.AdsPortals.RealityIdnesCz;
using RealEstatesWatcher.AdsPortals.RemaxCz;
using RealEstatesWatcher.AdsPortals.SrealityCz;
using RealEstatesWatcher.Core;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using RealEstatesWatcher.UI.Console.Settings;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace RealEstatesWatcher.UI.Console;

public class ConsoleRunner
{
    private static readonly AutoResetEvent WaitHandle = new(false);
    private static readonly CmdArguments CmdArguments = new();
    private static readonly ILogger<ConsoleRunner> Logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<ConsoleRunner>();
    
    private static IServiceProvider? _container;

    protected ConsoleRunner() { }

    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var parsed = await CmdArguments.ParseAsync(args);
        if (!parsed)
            return;

        
        Logger.LogInformation("------------------------------------------------");

        var configuration = WatcherConfiguration.Load(CmdArguments.ConfigFilePath);
        ConfigureDependencyInjection(configuration);
        if (_container is null)
        {
            Logger.LogCritical("Error starting Real estates Watcher: Unable to build all required components.");
            return;
        }

        try
        {
            var watcher = _container.GetRequiredService<RealEstatesWatchEngine>();
            RegisterAdsPortals(watcher, _container, configuration.PortalUrls);
            RegisterAdPostsHandlers(watcher, _container, configuration);
            RegisterAdPostsFilters(watcher, _container, configuration.BasicFilter);

            Logger.LogInformation("Starting Real estate Watcher engine...");

            // start watcher
            await watcher.StartAsync().ConfigureAwait(false);

            WaitForExitSignal();

            // stop watcher
            await watcher.StopAsync().ConfigureAwait(false);
        }
        catch (RealEstatesWatchEngineException reweEx)
        {
            Logger.LogCritical(reweEx, "Error starting Real estates Watcher: {Message}", reweEx.Message);
        }
    }

    private static void ConfigureDependencyInjection(WatcherConfiguration configuration)
    {
        var collection = new ServiceCollection();

        // add logging
        collection.AddLogging(builder =>
        {
            // NLog
            builder.AddNLog();

            // Sentry
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

            // Google Cloud Logging
            AddAndSetUpGoogleLogging(builder, configuration.GoogleCloud);

            // set Minimum log level based on variable in NLog.config --> default == INFO
            var minLevelVariable = LogManager.Configuration?.Variables["minLogLevel"].ToString();
            if (minLevelVariable is not null && Enum.TryParse(minLevelVariable, true, out LogLevel minLevel))
                builder.SetMinimumLevel(minLevel);
        });

        // add scraper
        if (configuration.WebScraper is not null)
        {
            collection.AddSingleton(configuration.WebScraper);
            collection.AddSingleton<IWebScraper, LocalNodejsConsoleWebScraper>();
        }

        // add engine
        collection.AddSingleton(configuration.Engine);
        collection.AddSingleton<RealEstatesWatchEngine>();

        _container = collection.BuildServiceProvider();
    }

    private static void AddAndSetUpGoogleLogging(ILoggingBuilder loggingBuilder, GoogleCloudSettings settings)
    {
        if (!settings.EnableCloudLogging)
            return;

        var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        var loggingOptions = LoggingOptions.Create(LogLevel.Debug, $"REW-{fileVersionInfo.FileVersion}", new Dictionary<string, string>
        {
            { "appId", $"App-{settings.ApplicationId ?? Guid.NewGuid().ToString()}" }
        });

#if DEBUG
        if (string.IsNullOrEmpty(settings.ProjectId))
            Logger.LogWarning("Google Cloud Logging is enabled and the app is running in DEBUG mode but Project ID is not set, logging might not work as expected.");
        if (string.IsNullOrEmpty(settings.ServiceName))
            Logger.LogWarning("Google Cloud Logging is enabled and the app is running in DEBUG mode but Service Name is not set, logging might not work as expected.");

        loggingBuilder.AddGoogle(new LoggingServiceOptions
        {
            ProjectId = settings.ProjectId,
            ServiceName = settings.ServiceName,
            Version = fileVersionInfo.FileVersion ?? "N/A",
            Options = loggingOptions
        });
#else
        loggingBuilder.AddGoogle(new LoggingServiceOptions
        {
            Options = loggingOptions
        });
#endif
    }

    private static void RegisterAdsPortals(RealEstatesWatchEngine watcher, IServiceProvider container, IEnumerable<string> urls)
    {
        Logger.LogInformation("Registering Ads portals..");

        foreach (var url in urls)
            RegisterAdsPortalInstanceByUrl(watcher, container, url);
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
            watcher.RegisterAdsPortal(new BezrealitkyCzAdsPortal(url, container.GetService<ILogger<BezrealitkyCzAdsPortal>>()));
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
            watcher.RegisterAdsPortal(new MmRealityCzAdsPortal(url,
                container.GetRequiredService<IWebScraper>(),
                container.GetService<ILogger<MmRealityCzAdsPortal>>()));
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
        if (url.Contains("remax-czech.cz"))
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

    private static void RegisterAdPostsHandlers(RealEstatesWatchEngine watcher, IServiceProvider container, WatcherConfiguration configuration)
    {
        Logger.LogInformation("Registering Ad posts handlers..");

        var spaceSeparatedNumberFormat = new NumberFormatInfo { NumberGroupSeparator = " " };
        watcher.RegisterAdPostsHandler(new EmailNotifyingAdPostsHandler(configuration.EmailHandler, spaceSeparatedNumberFormat, container.GetService<ILogger<EmailNotifyingAdPostsHandler>>()));
        watcher.RegisterAdPostsHandler(new LocalFileAdPostsHandler(configuration.FileHandler, spaceSeparatedNumberFormat));
    }

    private static void RegisterAdPostsFilters(RealEstatesWatchEngine watcher, IServiceProvider container, BasicParametersAdPostsFilterSettings? settings)
    {
        Logger.LogInformation("Registering Ad posts filters..");

        if (settings is null)
        {
            Logger.LogInformation("No filters provided.");
            return;
        }

        watcher.RegisterAdPostsFilter(new BasicParametersAdPostsFilter(settings, container.GetService<ILogger<BasicParametersAdPostsFilter>>()));
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
