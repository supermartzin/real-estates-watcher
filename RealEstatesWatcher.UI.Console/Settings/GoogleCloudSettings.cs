using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.UI.Console.Settings;

[SettingsSectionKey("gcloud")]
public record GoogleCloudSettings
{
    [SettingsKey("enable_cloud_logging")]
    public bool EnableCloudLogging { get; init; }

    [SettingsKey("application_id")]
    public string? ApplicationId { get; init; }

    [SettingsKey("project_id")]
    public string? ProjectId { get; init; }

    [SettingsKey("service_name")]
    public string? ServiceName { get; init; }
}