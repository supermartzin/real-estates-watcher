using System;

namespace RealEstatesWatcher.Scrapers
{
    public class WebScraperException : Exception
    {
        public WebScraperException()
        {
        }

        public WebScraperException(string message) : base(message)
        {
        }

        public WebScraperException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}