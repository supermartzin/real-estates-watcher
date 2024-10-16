namespace RealEstatesWatcher.AdPostsHandlers.Email;

public record EmailNotifyingAdPostsHandlerSettings
{
    public bool Enabled { get; set; }

    public string? FromAddress { get; set; }

    public string? SenderName { get; set; }

    public IEnumerable<string> ToAddresses { get; set; } = [];

    public IEnumerable<string> CcAddresses { get; set; } = [];
    
    public IEnumerable<string> BccAddresses { get; set; } = [];
        
    public string? SmtpServerHost { get; set; }

    public int SmtpServerPort { get; set; }

    public bool UseSecureConnection { get; set; }
        
    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool SkipInitialNotification { get; set; }
}