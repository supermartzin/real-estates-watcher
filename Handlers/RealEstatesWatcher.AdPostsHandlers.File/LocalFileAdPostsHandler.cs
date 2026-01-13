using RealEstatesWatcher.AdPostsHandlers.Base.Html;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.File;

public class LocalFileAdPostsHandler(LocalFileAdPostsHandlerSettings settings) : HtmlBasedAdPostsHandlerBase, IRealEstateAdPostsHandler
{
    private readonly LocalFileAdPostsHandlerSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public bool IsEnabled { get; } = settings.Enabled;

    public async Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default)
    {
        await HandleNewRealEstatesAdPostsAsync([adPost], cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        switch (_settings.PrintFormat)
        {
            case PrintFormat.PlainText:
                await WriteNewAdPostsToFileInPlainTextAsync(adPosts, cancellationToken).ConfigureAwait(false);
                break;

            case PrintFormat.Html:
                await WriteNewAdPostsToFileInHtmlAsync(adPosts, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        switch (_settings.PrintFormat)
        {
            case PrintFormat.PlainText:
                await WriteInitialAdPostsToFileInPlainTextAsync(adPosts, cancellationToken).ConfigureAwait(false);
                break;

            case PrintFormat.Html: 
                await WriteInitialAdPostsToFileInHtmlAsync(adPosts, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    #region Helpers

    private async Task WriteNewAdPostsToFileInPlainTextAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        var filePath = _settings.NewPostsToSeparateFile
            ? _settings.NewPostsFilePath
            : _settings.MainFilePath;

        var posts = string.Join(Environment.NewLine, adPosts);

        await WriteToFileAsync(filePath, posts, appendToFile: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteNewAdPostsToFileInHtmlAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        var filePath = _settings.NewPostsToSeparateFile
            ? _settings.NewPostsFilePath
            : _settings.MainFilePath;

        var pageContent = System.IO.File.Exists(filePath)
            ? await System.IO.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false)
            : CommonHtmlTemplateElements.FullPage.Replace("<maintitle/>", CommonHtmlTemplateElements.TitleNewPosts);

        var index = pageContent.IndexOf("<posts/>", StringComparison.Ordinal);
        var htmlPostsElements = string.Join(Environment.NewLine, adPosts.Select(CreateHtmlPostElement));

        // write to file, keeping the <posts/> element for future appends
        pageContent = pageContent.Insert(index + 8, htmlPostsElements + Environment.NewLine);

        await WriteToFileAsync(filePath, pageContent, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteInitialAdPostsToFileInPlainTextAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        var posts = string.Join(Environment.NewLine, adPosts);

        await WriteToFileAsync(_settings.MainFilePath, posts, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteInitialAdPostsToFileInHtmlAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        var posts = adPosts.Select(CreateHtmlPostElement);

        // insert title
        var page = CommonHtmlTemplateElements.FullPage.Replace("<maintitle/>", CommonHtmlTemplateElements.TitleInitialPosts);

        var index = page.IndexOf("<posts/>", StringComparison.Ordinal);
        page = page.Insert(index + 8, string.Join(Environment.NewLine, posts));

        await WriteToFileAsync(_settings.MainFilePath, page, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteToFileAsync(string? path, string content, CancellationToken cancellationToken = default) 
        => await WriteToFileAsync(path, content, false, cancellationToken).ConfigureAwait(false);

    private static async Task WriteToFileAsync(string? path, string content, bool appendToFile, CancellationToken cancellationToken = default)
    {
        if (path is null)
            throw new RealEstateAdPostsHandlerException("Error saving Ad posts to file: Path is not specified.");

        try
        {
            if (appendToFile)
            {
                await System.IO.File.AppendAllTextAsync(path, Environment.NewLine + content, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await System.IO.File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw new RealEstateAdPostsHandlerException($"Error saving Ad posts to file: {ex.Message}", ex);
        }
    }

    #endregion
}