namespace RealEstatesWatcher.AdPostsHandlers.Contracts
{
    public class RealEstateAdPostsHandlerException : Exception
    {
        public RealEstateAdPostsHandlerException() : base()
        {
        }

        public RealEstateAdPostsHandlerException(string message) : base(message)
        {
        }

        public RealEstateAdPostsHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}