using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.Scrapers;

[SettingsSectionKey("nodejs")]
public record LocalNodejsConsoleWebScraperSettings
{
    [SettingsKey("path_to_script")]
    public required string PathToScript { get; init; }

    [SettingsKey("page_scraping_timeout_seconds")]
    public required int PageScrapingTimeoutSeconds { get; init; }

    [SettingsKey("path_to_cookies_file")]
    public string? PathToCookiesFile { get; init; }
}