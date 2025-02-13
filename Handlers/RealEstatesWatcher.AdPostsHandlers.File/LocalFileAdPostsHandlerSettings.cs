namespace RealEstatesWatcher.AdPostsHandlers.File;

public record LocalFileAdPostsHandlerSettings
{
    public bool Enabled { get; init; }

    public string? MainFilePath { get; init; }

    public bool NewPostsToSeparateFile { get; init; }

    public string? NewPostsFilePath { get; init; }

    public PrintFormat PrintFormat { get; init; }
}