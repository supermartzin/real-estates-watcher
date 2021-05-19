namespace RealEstatesWatcher.AdPostsHandlers.File
{
    public class LocalFileAdPostsHandlerSettings
    {
        public bool Enabled { get; set; }

        public string? MainFilePath { get; set; }

        public bool NewPostsToSeparateFile { get; set; }

        public string? NewPostsFilePath { get; set; }
    }
}