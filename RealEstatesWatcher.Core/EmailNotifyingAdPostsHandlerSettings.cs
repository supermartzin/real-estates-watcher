namespace RealEstatesWatcher.Core
{
    public class EmailNotifyingAdPostsHandlerSettings
    {
        public string? EmailAddressFrom { get; set; }

        public string? SenderName { get; set; }
        
        public string? EmailAddressTo { get; set; }
        
        public string? RecipientName { get; set; }

        public string? SmtpServerHost { get; set; }

        public int SmtpServerPort { get; set; }

        public bool UseSecureConnection { get; set; }
        
        public string? Username { get; set; }

        public string? Password { get; set; }
    }
}