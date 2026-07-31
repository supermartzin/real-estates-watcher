using System.Globalization;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdPostsHandlers.File;

namespace RealEstatesWatcher.Tests;

public sealed class LocalFileAdPostsHandlerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"real-estates-watcher-tests-{Guid.NewGuid():N}");

    public LocalFileAdPostsHandlerTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task PlainText_InitialWriteReplacesFileAndNewPostsAppend()
    {
        var path = Path.Combine(_directory, "posts.txt");
        await File.WriteAllTextAsync(path, "old content");
        var handler = CreateHandler(path, PrintFormat.PlainText);

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost("initial")]);
        await handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost("new", title: "New listing"));
        var content = await File.ReadAllTextAsync(path);

        Assert.DoesNotContain("old content", content);
        Assert.Contains("/initial", content);
        Assert.Contains("New listing", content);
        Assert.Contains("/new", content);
    }

    [Fact]
    public async Task NewPosts_CanBeWrittenToASeparateFile()
    {
        var mainPath = Path.Combine(_directory, "all.txt");
        var newPath = Path.Combine(_directory, "new.txt");
        var handler = new LocalFileAdPostsHandler(new LocalFileAdPostsHandlerSettings
        {
            Enabled = true,
            MainFilePath = mainPath,
            NewPostsToSeparateFile = true,
            NewPostsFilePath = newPath,
            PrintFormat = PrintFormat.PlainText
        });

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost("initial")]);
        await handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost("new"));

        Assert.Contains("/initial", await File.ReadAllTextAsync(mainPath));
        Assert.DoesNotContain("/new", await File.ReadAllTextAsync(mainPath));
        Assert.Contains("/new", await File.ReadAllTextAsync(newPath));
    }

    [Fact]
    public async Task Html_NewPostsAreInsertedBeforeInitialPosts()
    {
        var path = Path.Combine(_directory, "posts.html");
        var handler = CreateHandler(path, PrintFormat.Html);

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost("initial", title: "Initial listing")]);
        await handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost("new", title: "New listing"));
        var content = await File.ReadAllTextAsync(path);

        Assert.StartsWith("<!DOCTYPE html>", content.TrimStart());
        Assert.True(content.IndexOf("New listing", StringComparison.Ordinal) < content.IndexOf("Initial listing", StringComparison.Ordinal));
        Assert.Contains("<posts/>", content);
    }

    [Fact]
    public async Task Html_FirstNewPostCreatesACompletePage()
    {
        var path = Path.Combine(_directory, "new-only.html");
        var handler = CreateHandler(path, PrintFormat.Html);

        await handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost("new-only"));
        var content = await File.ReadAllTextAsync(path);

        Assert.StartsWith("<!DOCTYPE html>", content.TrimStart());
        Assert.Contains("/new-only", content);
        Assert.Contains("<posts/>", content);
    }

    [Fact]
    public async Task InvalidDirectory_WrapsFileSystemFailure()
    {
        var path = Path.Combine(_directory, "missing", "posts.txt");
        var handler = CreateHandler(path, PrintFormat.PlainText);

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost()]));

        Assert.IsType<DirectoryNotFoundException>(exception.InnerException);
    }

    [Fact]
    public async Task MissingPrintFormat_PerformsNoWrite()
    {
        var path = Path.Combine(_directory, "unused.txt");
        var handler = new LocalFileAdPostsHandler(new LocalFileAdPostsHandlerSettings
        {
            Enabled = true,
            MainFilePath = path,
            PrintFormat = null
        });

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost()]);
        await handler.HandleNewRealEstatesAdPostsAsync([TestData.CreatePost("new")]);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task MissingPath_ThrowsDomainException()
    {
        var handler = new LocalFileAdPostsHandler(new LocalFileAdPostsHandlerSettings
        {
            Enabled = true,
            MainFilePath = null,
            PrintFormat = PrintFormat.PlainText
        });

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost()]));

        Assert.Contains("Path is not specified", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_ReflectsSettings(bool enabled)
    {
        var handler = new LocalFileAdPostsHandler(new LocalFileAdPostsHandlerSettings { Enabled = enabled });

        Assert.Equal(enabled, handler.IsEnabled);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static LocalFileAdPostsHandler CreateHandler(string path, PrintFormat format) => new(
        new LocalFileAdPostsHandlerSettings
        {
            Enabled = true,
            MainFilePath = path,
            PrintFormat = format
        },
        new NumberFormatInfo { NumberDecimalDigits = 0, NumberGroupSeparator = " " });
}
