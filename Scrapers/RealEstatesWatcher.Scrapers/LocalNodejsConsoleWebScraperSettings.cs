namespace RealEstatesWatcher.Scrapers;

public record LocalNodejsConsoleWebScraperSettings
{
    public required string PathToScript { get; set; }

    public required int PageScrapingTimeoutSeconds { get; set; }

    public string? PathToCookiesFile { get; set; }
}