using System;
using System.Threading.Tasks;

namespace RealEstatesWatcher.AdsPortals.Contracts
{
    public interface IWebScraper
    {
        Task<string> GetFullWebPageContentAsync(string url);

        Task<string> GetFullWebPageContentAsync(Uri uri);
    }
}