using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

[SettingsSectionKey("email")]
public record EmailNotifyingAdPostsHandlerSettings
{
    [SettingsKey("enabled")]
    public bool Enabled { get; init; }
    
    [SettingsKey("from")]
    public string? FromAddress { get; init; }
    
    [SettingsKey("sender_name")]
    public string? SenderName { get; init; }

    [SettingsKey("to")]
    public IEnumerable<string> ToAddresses { get; init; } = [];
    
    [SettingsKey("cc")]
    public IEnumerable<string> CcAddresses { get; init; } = [];

    [SettingsKey("bcc")]
    public IEnumerable<string> BccAddresses { get; init; } = [];
        
    [SettingsKey("smtp_server_host")]
    public string? SmtpServerHost { get; init; }

    [SettingsKey("smtp_server_port")]
    public int? SmtpServerPort { get; init; }

    [SettingsKey("use_secure_connection")]
    public bool? UseSecureConnection { get; init; } = true;

    [SettingsKey("username")]
    public string? Username { get; init; }
    
    [SettingsKey("password")]
    public string? Password { get; init; }

    [SettingsKey("skip_initial_notification")]
    public bool? SkipInitialNotification { get; init; } = false;
}