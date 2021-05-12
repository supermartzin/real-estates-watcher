using System.Collections.Generic;

namespace RealEstatesWatcher.AdPostsHandlers.Email
{
    public class EmailNotifyingAdPostsHandlerSettings
    {
        public string? EmailAddressFrom { get; set; }

        public string? SenderName { get; set; }
        
        public IEnumerable<string> EmailAddressesTo { get; set; }
        
        public string? SmtpServerHost { get; set; }

        public int SmtpServerPort { get; set; }

        public bool UseSecureConnection { get; set; }
        
        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool SkipInitialNotification { get; set; }

        public EmailNotifyingAdPostsHandlerSettings()
        {
            EmailAddressesTo = new List<string>();
        }
    }
}