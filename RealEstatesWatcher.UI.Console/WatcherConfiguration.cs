using Microsoft.Extensions.Configuration;
using RealEstatesWatcher.AdPostsFilters.BasicFilter;
using RealEstatesWatcher.AdPostsHandlers.Email;
using RealEstatesWatcher.AdPostsHandlers.File;
using RealEstatesWatcher.Core;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Tools.Attributes;
using RealEstatesWatcher.UI.Console.Settings;

namespace RealEstatesWatcher.UI.Console;

public sealed class WatcherConfiguration
{
    public WatchEngineSettings Engine { get; }
    public GoogleCloudSettings GoogleCloud { get; }
    public EmailNotifyingAdPostsHandlerSettings EmailHandler { get; }
    public LocalFileAdPostsHandlerSettings FileHandler { get; }
    public BasicParametersAdPostsFilterSettings? BasicFilter { get; }
    public LocalNodejsConsoleWebScraperSettings? WebScraper { get; }
    public IReadOnlyList<string> PortalUrls { get; }

    private WatcherConfiguration(IConfiguration configuration)
    {
        Engine = LoadWatchEngineSettings(configuration.GetSection(Attributes.GetSettingsSectionKey<WatchEngineSettings>()));
        GoogleCloud = LoadGoogleCloudSettings(configuration.GetSection(Attributes.GetSettingsSectionKey<GoogleCloudSettings>()));
        EmailHandler = LoadEmailSettings(configuration.GetSection(Attributes.GetSettingsSectionKey<EmailNotifyingAdPostsHandlerSettings>()));
        FileHandler = LoadFileSettings(configuration.GetSection(Attributes.GetSettingsSectionKey<LocalFileAdPostsHandlerSettings>()));

        var filterSection = configuration.GetSection(Attributes.GetSettingsSectionKey<BasicParametersAdPostsFilterSettings>());
        BasicFilter = filterSection.Exists() ? LoadBasicFilterSettings(filterSection) : null;

        var scraperSection = configuration.GetSection(Attributes.GetSettingsSectionKey<LocalNodejsConsoleWebScraperSettings>());
        WebScraper = scraperSection.Exists() ? LoadWebScraperSettings(scraperSection) : null;

        PortalUrls = configuration.GetSection("portals").GetChildren()
            .Select(entry => entry.Value?.Trim())
            .Where(value => !string.IsNullOrEmpty(value))
            .Select(value => value!)
            .ToArray();
    }

    public static WatcherConfiguration Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var configuration = new ConfigurationBuilder()
            .AddIniFile(Path.GetFullPath(filePath))
            .Build();

        using (configuration as IDisposable)
            return new WatcherConfiguration(configuration);
    }

    private static WatchEngineSettings LoadWatchEngineSettings(IConfiguration configuration) => new()
    {
        PerformCheckOnStartup = configuration.GetValue(
            Attributes.GetSettingsKey<WatchEngineSettings>(nameof(WatchEngineSettings.PerformCheckOnStartup)), true),
        EnableMultiplePortalInstances = configuration.GetValue(
            Attributes.GetSettingsKey<WatchEngineSettings>(nameof(WatchEngineSettings.EnableMultiplePortalInstances)), true),
        CheckIntervalMinutes = configuration.GetValue<int>(
            Attributes.GetSettingsKey<WatchEngineSettings>(nameof(WatchEngineSettings.CheckIntervalMinutes))),
        StartCheckAtSpecificTime = configuration.GetValue<TimeOnly?>(
            Attributes.GetSettingsKey<WatchEngineSettings>(nameof(WatchEngineSettings.StartCheckAtSpecificTime)))
    };

    private static GoogleCloudSettings LoadGoogleCloudSettings(IConfiguration configuration) => new()
    {
        EnableCloudLogging = configuration.GetValue<bool>(Attributes.GetSettingsKey<GoogleCloudSettings>(nameof(GoogleCloudSettings.EnableCloudLogging))),
        ApplicationId = configuration.GetValue<string?>(Attributes.GetSettingsKey<GoogleCloudSettings>(nameof(GoogleCloudSettings.ApplicationId))),
        ProjectId = configuration.GetValue<string?>(Attributes.GetSettingsKey<GoogleCloudSettings>(nameof(GoogleCloudSettings.ProjectId))),
        ServiceName = configuration.GetValue<string?>(Attributes.GetSettingsKey<GoogleCloudSettings>(nameof(GoogleCloudSettings.ServiceName)))
    };

    private static EmailNotifyingAdPostsHandlerSettings LoadEmailSettings(IConfiguration configuration) => new()
    {
        Enabled = configuration.GetValue<bool>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.Enabled))),
        FromAddress = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.FromAddress))),
        ToAddresses = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.ToAddresses)))
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
        CcAddresses = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.CcAddresses)))
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
        BccAddresses = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.BccAddresses)))
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
        SenderName = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.SenderName))),
        SmtpServerHost = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.SmtpServerHost))),
        SmtpServerPort = configuration.GetValue<int?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.SmtpServerPort))),
        UseSecureConnection = configuration.GetValue<bool?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.UseSecureConnection))),
        Username = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.Username))),
        Password = configuration.GetValue<string?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.Password))),
        SkipInitialNotification = configuration.GetValue<bool?>(Attributes.GetSettingsKey<EmailNotifyingAdPostsHandlerSettings>(nameof(EmailNotifyingAdPostsHandlerSettings.SkipInitialNotification)))
    };

    private static LocalFileAdPostsHandlerSettings LoadFileSettings(IConfiguration configuration) => new()
    {
        Enabled = configuration.GetValue<bool>(Attributes.GetSettingsKey<LocalFileAdPostsHandlerSettings>(nameof(LocalFileAdPostsHandlerSettings.Enabled))),
        MainFilePath = configuration.GetValue<string?>(Attributes.GetSettingsKey<LocalFileAdPostsHandlerSettings>(nameof(LocalFileAdPostsHandlerSettings.MainFilePath))),
        NewPostsToSeparateFile = configuration.GetValue<bool?>(Attributes.GetSettingsKey<LocalFileAdPostsHandlerSettings>(nameof(LocalFileAdPostsHandlerSettings.NewPostsToSeparateFile))),
        NewPostsFilePath = configuration.GetValue<string?>(Attributes.GetSettingsKey<LocalFileAdPostsHandlerSettings>(nameof(LocalFileAdPostsHandlerSettings.NewPostsFilePath))),
        PrintFormat = Enum.TryParse(configuration.GetValue<string?>(Attributes.GetSettingsKey<LocalFileAdPostsHandlerSettings>(nameof(LocalFileAdPostsHandlerSettings.PrintFormat))),
            ignoreCase: true, out PrintFormat format) ? format : PrintFormat.PlainText
    };

    private static BasicParametersAdPostsFilterSettings LoadBasicFilterSettings(IConfiguration configuration) => new()
    {
        MinPrice = configuration.GetValue<decimal?>(Attributes.GetSettingsKey<BasicParametersAdPostsFilterSettings>(nameof(BasicParametersAdPostsFilterSettings.MinPrice))),
        MaxPrice = configuration.GetValue<decimal?>(Attributes.GetSettingsKey<BasicParametersAdPostsFilterSettings>(nameof(BasicParametersAdPostsFilterSettings.MaxPrice))),
        MinFloorArea = configuration.GetValue<decimal?>(Attributes.GetSettingsKey<BasicParametersAdPostsFilterSettings>(nameof(BasicParametersAdPostsFilterSettings.MinFloorArea))),
        MaxFloorArea = configuration.GetValue<decimal?>(Attributes.GetSettingsKey<BasicParametersAdPostsFilterSettings>(nameof(BasicParametersAdPostsFilterSettings.MaxFloorArea))),
        Layouts = ParseLayouts(configuration.GetValue<string?>(Attributes.GetSettingsKey<BasicParametersAdPostsFilterSettings>(nameof(BasicParametersAdPostsFilterSettings.Layouts))))
    };

    private static ISet<Layout> ParseLayouts(string? layoutsValue)
    {
        if (string.IsNullOrWhiteSpace(layoutsValue))
            return new HashSet<Layout>();

        var layouts = layoutsValue.Split(',').Select(LayoutExtensions.ToLayout).ToHashSet();
        // Include ads without an explicit layout whenever layout filtering is enabled.
        layouts.Add(Layout.NotSpecified);
        return layouts;
    }

    private static LocalNodejsConsoleWebScraperSettings LoadWebScraperSettings(IConfiguration configuration) => new()
    {
        PathToNodeExecutable = configuration.GetValue<string?>(Attributes.GetSettingsKey<LocalNodejsConsoleWebScraperSettings>(nameof(LocalNodejsConsoleWebScraperSettings.PathToNodeExecutable))),
        PathToScript = configuration.GetValue(Attributes.GetSettingsKey<LocalNodejsConsoleWebScraperSettings>(nameof(LocalNodejsConsoleWebScraperSettings.PathToScript)), string.Empty),
        PageScrapingTimeoutSeconds = configuration.GetValue<int>(Attributes.GetSettingsKey<LocalNodejsConsoleWebScraperSettings>(nameof(LocalNodejsConsoleWebScraperSettings.PageScrapingTimeoutSeconds))),
        PathToCookiesFile = configuration.GetValue<string?>(Attributes.GetSettingsKey<LocalNodejsConsoleWebScraperSettings>(nameof(LocalNodejsConsoleWebScraperSettings.PathToCookiesFile)))
    };
}
