using RealEstatesWatcher.Tools.Attributes;

namespace RealEstatesWatcher.AdPostsHandlers.File;

[SettingsSectionKey("file")]
public record LocalFileAdPostsHandlerSettings
{
    [SettingsKey("enabled")]
    public bool Enabled { get; init; }

    [SettingsKey("main_path")]
    public string? MainFilePath { get; init; }

    [SettingsKey("separate_new_posts")]
    public bool? NewPostsToSeparateFile { get; init; } = false;
    
    [SettingsKey("new_posts_path")]
    public string? NewPostsFilePath { get; init; }

    [SettingsKey("print_format")]
    public PrintFormat? PrintFormat { get; init; } = File.PrintFormat.PlainText;
}