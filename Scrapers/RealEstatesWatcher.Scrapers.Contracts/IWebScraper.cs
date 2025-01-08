using System.Text;

namespace RealEstatesWatcher.Scrapers.Contracts;

public interface IWebScraper
{
    Task<string> GetFullWebPageContentAsync(string url, Encoding? pageEncoding = null, CancellationToken cancellationToken = default);

    Task<string> GetFullWebPageContentAsync(Uri uri, Encoding? pageEncoding = null, CancellationToken cancellationToken = default);
}