using System;

namespace RealEstatesWatcher.AdsPortals.Contracts
{
    public class RealEstateAdsPortalException : Exception
    {
        public RealEstateAdsPortalException() : base()
        {
        }

        public RealEstateAdsPortalException(string message) : base(message)
        {
        }

        public RealEstateAdsPortalException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}