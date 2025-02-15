﻿using System.Globalization;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.File;

public class LocalFileAdPostsHandler(LocalFileAdPostsHandlerSettings settings) : IRealEstateAdPostsHandler
{
    private static class HtmlTemplates
    {
        public const string FullPage = """
                                       <!DOCTYPE html>
                                       <html lang="en">
                                       <head>
                                           <meta charset="utf-8">
                                           <title>Real Estate Advertisements</title>
                                       </head>
                                       <body style="max-width: 800px; margin:10px auto;">
                                           <maintitle/>
                                           <posts/>
                                       </body>
                                       </html>
                                       """;

        public const string TitleNewPosts = """<h1>🏦 <span style="color: #4f4f4f; font-style: italic;">NEW Real estate offer</span></h1>""";

        public const string TitleInitialPosts = """<h1>🏦 <span style="color: #4f4f4f; font-style: italic;"> Current Real estate offer</span></h1>""";

        public const string Post = "<div style=\"padding: 10px; background: #ededed; min-height: 200px;\">\r\n    <div style=\"float: left; margin-right: 1em; width: 30%; height: 180px; display: {$img-display};\">\r\n        <img src=\"{$img-link}\" style=\"height: 100%; width: 100%; object-fit: cover;\" />\r\n    </div>\r\n    <a href=\"{$post-link}\">\r\n        <h3 style=\"margin: 0.2em; margin-top: 0;\">{$title}</h3>\r\n    </a>\r\n    <span style=\"font-size: medium; color: #4f4f4f; display: {$price-display};\">\r\n        <strong>{$price}</strong> {$currency}\r\n        <span style=\"display: {$additional-fees-display};\"> + {$additional-fees} {$currency}</span><br/>\r\n    </span>\r\n    <span style=\"font-size: medium; color: #4f4f4f; display: {$price-comment-display};\">\r\n        <strong>{$price-comment}</strong><br/>\r\n    </span>\r\n    <span>\r\n        <strong>Server:</strong> {$portal-name}<br/>\r\n        <strong>Adresa:</strong> {$address}<br/>\r\n        <strong>Výmera:</strong> {$floor-area}<br/>\r\n        <strong>Dispozícia:</strong> {$layout}</br>\r\n    </span>\r\n    <p style=\"margin: 0.2em; font-size: small; text-align: justify; display: {$text-display};\">{$text}</p>\r\n</div>";
    }

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
            pageContent = HtmlTemplates.FullPage.Replace("<maintitle/>", HtmlTemplates.TitleNewPosts);

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
        var page = HtmlTemplates.FullPage.Replace("<maintitle/>", HtmlTemplates.TitleInitialPosts);

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
        var postHtml = HtmlTemplates.Post
            .Replace("{$title}", post.Title)
            .Replace("{$portal-name}", post.AdsPortalName)
            .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
            .Replace("{$address}", post.Address);

        // layout
        postHtml = postHtml.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtml = post.FloorArea is not null and not decimal.Zero ? postHtml.Replace("{$floor-area}", post.FloorArea + " m²") : postHtml.Replace("{$floor-area}", " -");

        // image
        if (post.ImageUrl is not null)
        {
            postHtml = postHtml.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                .Replace("{$img-display}", "block");
        }
        else
        {
            postHtml = postHtml.Replace("{$img-display}", "none");
        }

        // price
        if (post.Price is not decimal.Zero)
        {
            postHtml = postHtml.Replace("{$price}", post.Price.ToString("N", new NumberFormatInfo {NumberGroupSeparator = " "}))
                .Replace("{$currency}", post.Currency.ToString())
                .Replace("{$price-display}", "block")
                .Replace("{$price-comment-display}", "none");
        }
        else
        {
            postHtml = postHtml.Replace("{$price-comment}", post.PriceComment ?? "-")
                .Replace("{$price-display}", "none")
                .Replace("{$price-comment-display}", "block");
        }

        // additional fees
        if (post.AdditionalFees is not null and not decimal.Zero)
        {
            postHtml = postHtml.Replace("{$additional-fees}", post.AdditionalFees.Value.ToString("N", new NumberFormatInfo {NumberGroupSeparator = " "}))
                .Replace("{$additional-fees-display}", "inline-block");
        }
        else
        {
            postHtml = postHtml.Replace("{$additional-fees-display}", "none");
        }

        // text
        if (!string.IsNullOrEmpty(post.Text))
        {
            postHtml = postHtml.Replace("{$text}", post.Text)
                .Replace("{$text-display}", "table");
        }
        else
        {
            postHtml = postHtml.Replace("{$text-display}", "none");
        }

        return postHtml;
    }

    #endregion
}