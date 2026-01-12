using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Templates;
using RealEstatesWatcher.Models;

using System.Globalization;
using System.Text;
using System.Web;

namespace RealEstatesWatcher.AdPostsHandlers.File;

public class LocalFileAdPostsHandler(LocalFileAdPostsHandlerSettings settings) : IRealEstateAdPostsHandler
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

        var htmlPostsElements = string.Join(Environment.NewLine, adPosts.Select(CreateHtmlPostElement));

        string pageContent;
        if (System.IO.File.Exists(filePath))
        {
            // read content of existing file
            var fileContents = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            var index = fileContents.IndexOf("<posts/>", StringComparison.Ordinal);

            pageContent = fileContents.Insert(index + 8, htmlPostsElements + Environment.NewLine);
        }
        else
        {
            // create new content
            pageContent = CommonHtmlTemplateElements.FullPage.Replace("<maintitle/>", CommonHtmlTemplateElements.TitleNewPosts);

            var index = pageContent.IndexOf("<posts/>", StringComparison.Ordinal);
            pageContent = pageContent.Insert(index + 8, htmlPostsElements + Environment.NewLine);
        }

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

    private static string CreateHtmlPostElement(RealEstateAdPost post)
    {
        var postHtmlBuilder = new StringBuilder(CommonHtmlTemplateElements.Post)
            .Replace("{$title}", post.Title)
            .Replace("{$portal-name}", post.AdsPortalName)
            .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
            .Replace("{$address}", post.Address);

        // address links
        if (!string.IsNullOrEmpty(post.Address))
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$address-links-display}", "inline-block")
                .Replace("{$address-encoded}", HttpUtility.UrlEncode(post.Address));
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$address-links-display}", "none");
        }

        // layout
        postHtmlBuilder = postHtmlBuilder.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtmlBuilder = post.FloorArea is not null and not decimal.Zero ? postHtmlBuilder.Replace("{$floor-area}", post.FloorArea + " m²") : postHtml.Replace("{$floor-area}", " -");

        // image
        if (post.ImageUrl is not null)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                .Replace("{$img-display}", "block");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$img-display}", "none");
        }

        // price
        if (post.Price is not decimal.Zero)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$price}", post.Price.ToString("N", new NumberFormatInfo {NumberGroupSeparator = " "}))
                .Replace("{$currency}", post.Currency.ToString())
                .Replace("{$price-display}", "block")
                .Replace("{$price-comment-display}", "none");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$price-comment}", post.PriceComment ?? "-")
                .Replace("{$price-display}", "none")
                .Replace("{$price-comment-display}", "block");
        }

        // additional fees
        if (post.AdditionalFees is not null and not decimal.Zero)
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$additional-fees}", post.AdditionalFees.Value.ToString("N", new NumberFormatInfo {NumberGroupSeparator = " "}))
                .Replace("{$additional-fees-display}", "inline-block");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$additional-fees-display}", "none");
        }

        // text
        if (!string.IsNullOrEmpty(post.Text))
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$text}", post.Text)
                .Replace("{$text-display}", "table");
        }
        else
        {
            postHtmlBuilder = postHtmlBuilder.Replace("{$text-display}", "none");
        }

        return postHtmlBuilder.ToString();
    }

    #endregion
}