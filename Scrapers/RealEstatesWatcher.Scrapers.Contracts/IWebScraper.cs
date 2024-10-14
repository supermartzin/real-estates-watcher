namespace RealEstatesWatcher.Scrapers.Contracts
{
    public interface IWebScraper
    {
        Task<string> GetFullWebPageContentAsync(string url);

        Task<string> GetFullWebPageContentAsync(Uri uri);
    }
}