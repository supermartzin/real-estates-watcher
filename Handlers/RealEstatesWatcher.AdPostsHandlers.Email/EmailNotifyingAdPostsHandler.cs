using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

public class EmailNotifyingAdPostsHandler(EmailNotifyingAdPostsHandlerSettings settings,
                                          ILogger<EmailNotifyingAdPostsHandler>? logger = default) : IRealEstateAdPostsHandler
{
    private static class HtmlTemplates
    {
        public const string FullPage = """
                                       <!DOCTYPE html>
                                       <html>
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

    private readonly EmailNotifyingAdPostsHandlerSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
    public bool IsEnabled { get; } = settings.Enabled;

    public async Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost)
    {
        ArgumentNullException.ThrowIfNull(adPost);

        logger?.LogDebug("Received new Real Estate Ad Post: {Post}", adPost);

        await SendEmail("🆕 New Real Estate Advert published!", CreateHtmlBody(adPost, HtmlTemplates.TitleNewPosts)).ConfigureAwait(false);
    }

    public async Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        logger?.LogDebug("Received '{PostsCount}' new Real Estate Ad Posts.", adPosts.Count);

        await SendEmail("🆕 New Real Estate Adverts published!", CreateHtmlBody(adPosts, HtmlTemplates.TitleNewPosts)).ConfigureAwait(false);
    }

    public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        if (_settings.SkipInitialNotification)
        {
            logger?.LogDebug("Skipping initial notification on {PostsCount} Real Estate Ad posts", adPosts.Count);
            return;
        }
            
        logger?.LogDebug("Received initial {PostsCount} Real Estate Ad Posts.", adPosts.Count);

        await SendEmail("🏦 Current Real Estate Adverts offering", CreateHtmlBody(adPosts, HtmlTemplates.TitleInitialPosts)).ConfigureAwait(false);
    }

    private async Task SendEmail(string subject, string body)
    {
        var message = new MimeMessage
        {
            Subject = subject,
            Body = new BodyBuilder { HtmlBody = body }.ToMessageBody(),
            From =
            {
                new MailboxAddress(_settings.SenderName, _settings.FromAddress)
            }
        };
        message.To.AddRange(_settings.ToAddresses.Select(address => new MailboxAddress(address, address)));
        message.Cc.AddRange(_settings.CcAddresses.Select(address => new MailboxAddress(address, address)));
        message.Bcc.AddRange(_settings.BccAddresses.Select(address => new MailboxAddress(address, address)));

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.SmtpServerHost,
                _settings.SmtpServerPort,
                _settings.UseSecureConnection).ConfigureAwait(false);

            await client.AuthenticateAsync(new NetworkCredential(_settings.Username,
                _settings.Password)).ConfigureAwait(false);

            // send email
            await client.SendAsync(message).ConfigureAwait(false);
            await client.DisconnectAsync(true).ConfigureAwait(false);

            logger?.LogInformation("Notification email has been successfully sent.");
        }
        catch (Exception ex)
        {
            throw new RealEstateAdPostsHandlerException($"Error during sending email notification: {ex.Message}", ex);
        }
    }

    private static string CreateHtmlBody(RealEstateAdPost adPost, string titleHtmlElement)
    {
        return HtmlTemplates.FullPage.Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", CreateSingleHtmlPost(adPost));
    }

    private static string CreateHtmlBody(IEnumerable<RealEstateAdPost> adPosts, string titleHtmlElement)
    {
        return HtmlTemplates.FullPage.Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", string.Join(Environment.NewLine, adPosts.Select(CreateSingleHtmlPost)));
    }

    private static string CreateSingleHtmlPost(RealEstateAdPost post)
    {
        var postHtml = HtmlTemplates.Post
            .Replace("{$title}", post.Title)
            .Replace("{$portal-name}", post.AdsPortalName)
            .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
            .Replace("{$address}", post.Address);

        // layout
        postHtml = postHtml.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtml = post.FloorArea is not decimal.Zero ? postHtml.Replace("{$floor-area}", post.FloorArea + " m²") : postHtml.Replace("{$floor-area}", " -");
            
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
        if (post.AdditionalFees is not decimal.Zero)
        {
            postHtml = postHtml.Replace("{$additional-fees}", post.AdditionalFees.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
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
}