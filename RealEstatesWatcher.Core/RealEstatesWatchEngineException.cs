using System;

namespace RealEstatesWatcher.Core
{
    public class RealEstatesWatchEngineException : Exception
    {
        public RealEstatesWatchEngineException() : base()
        {
        }

        public RealEstatesWatchEngineException(string message) : base(message)
        {
        }

        public RealEstatesWatchEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}