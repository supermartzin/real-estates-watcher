using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.Core;

[SettingsSectionKey("general")]
public record WatchEngineSettings
{

    [SettingsKey("perform_check_on_startup")]
    public bool PerformCheckOnStartup { get; init; }

    [SettingsKey("enable_multiple_portal_instances")]
    public bool EnableMultiplePortalInstances { get; init; }

    [SettingsKey("check_interval_minutes")]
    public int CheckIntervalMinutes { get; init; }

    [SettingsKey("start_periodic_check_at")]
    public TimeOnly? StartCheckAtSpecificTime { get; init; }
}