using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.Core;

[SettingsSectionKey("general")]
public record WatchEngineSettings
{ 
    [SettingsKey("check_interval_minutes")]
    public int CheckIntervalMinutes { get; init; }

    [SettingsKey("enable_multiple_portal_instances")]
    public bool EnableMultiplePortalInstances { get; init; }
}