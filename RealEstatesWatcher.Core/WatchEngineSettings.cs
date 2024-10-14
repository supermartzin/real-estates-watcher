namespace RealEstatesWatcher.Core;

public record WatchEngineSettings
{
    public int CheckIntervalMinutes { get; set; }

    public bool EnableMultiplePortalInstances { get; set; }
}