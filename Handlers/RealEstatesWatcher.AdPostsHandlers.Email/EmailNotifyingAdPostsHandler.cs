using System.Globalization;
using System.Net;
using System.Web;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Templates;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

public class EmailNotifyingAdPostsHandler(EmailNotifyingAdPostsHandlerSettings settings,
                                          ILogger<EmailNotifyingAdPostsHandler>? logger = null) : IRealEstateAdPostsHandler
{
    private readonly EmailNotifyingAdPostsHandlerSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public bool IsEnabled { get; } = settings.Enabled;

    public async Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPost);

        logger?.LogDebug("Received new Real Estate Ad Post: {Post}", adPost);

        await SendEmailAsync("🆕 New Real Estate Advert published!", CreateHtmlBody(adPost, CommonHtmlTemplateElements.TitleNewPosts), cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        logger?.LogDebug("Received '{PostsCount}' new Real Estate Ad Posts.", adPosts.Count);

        await SendEmailAsync("🆕 New Real Estate Adverts published!", CreateHtmlBody(adPosts, CommonHtmlTemplateElements.TitleNewPosts), cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        if (_settings.SkipInitialNotification)
        {
            logger?.LogDebug("Skipping initial notification on {PostsCount} Real Estate Ad posts", adPosts.Count);
            return;
        }

        logger?.LogDebug("Received initial {PostsCount} Real Estate Ad Posts.", adPosts.Count);

        await SendEmailAsync("🏦 Current Real Estate Adverts offering", CreateHtmlBody(adPosts, CommonHtmlTemplateElements.TitleInitialPosts), cancellationToken).ConfigureAwait(false);
    }

    private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken = default)
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
        if (_settings.CcAddresses.Any())
            message.Cc.AddRange(_settings.CcAddresses.Select(address => new MailboxAddress(address, address)));
        if (_settings.BccAddresses.Any())
            message.Bcc.AddRange(_settings.BccAddresses.Select(address => new MailboxAddress(address, address)));

        try
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.SmtpServerHost,
                _settings.SmtpServerPort,
                _settings.UseSecureConnection, cancellationToken).ConfigureAwait(false);

            await client.AuthenticateAsync(new NetworkCredential(_settings.Username,
                _settings.Password), cancellationToken).ConfigureAwait(false);

            // send email
            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            logger?.LogInformation("Notification email has been successfully sent.");
        }
        catch (Exception ex)
        {
            throw new RealEstateAdPostsHandlerException($"Error during sending email notification: {ex.Message}", ex);
        }
    }

    private static string CreateHtmlBody(RealEstateAdPost adPost, string titleHtmlElement)
    {
        return CommonHtmlTemplateElements.FullPage.Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", CreateSingleHtmlPost(adPost));
    }

    private static string CreateHtmlBody(IEnumerable<RealEstateAdPost> adPosts, string titleHtmlElement)
    {
        return CommonHtmlTemplateElements.FullPage.Replace("<maintitle/>", titleHtmlElement)
            .Replace("<posts/>", string.Join(Environment.NewLine, adPosts.Select(CreateSingleHtmlPost)));
    }

    private static string CreateSingleHtmlPost(RealEstateAdPost post)
    {
        var postHtml = CommonHtmlTemplateElements.Post
            .Replace("{$title}", post.Title)
            .Replace("{$portal-name}", post.AdsPortalName)
            .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
            .Replace("{$address}", post.Address);

        // address links
        if (!string.IsNullOrEmpty(post.Address))
        {
            postHtml = postHtml.Replace("{$address-links-display}", "inline-block")
                               .Replace("{$address-encoded}", HttpUtility.UrlEncode(post.Address));
        }
        else
        {
            postHtml = postHtml.Replace("{$address-links-display}", "none");
        }

        // layout
        postHtml = postHtml.Replace("{$layout}", post.Layout is not Layout.NotSpecified ? post.Layout.ToDisplayString() : "-");

        // floor area
        postHtml = post.FloorArea is not null and not decimal.Zero ? postHtml.Replace("{$floor-area}", post.FloorArea + " m²") : postHtml.Replace("{$floor-area}", "-");

        // image
        if (post.ImageUrl is not null)
        {
            postHtml = postHtml.Replace("{$img-link}", post.ImageUrl.AbsoluteUri)
                               .Replace("{$img-display}", "block");
        }
        else
        {
            postHtml = postHtml.Replace("{$img-link}", string.Empty)
                               .Replace("{$img-display}", "none");
        }

        // price
        if (post.Price is not decimal.Zero)
        {
            postHtml = postHtml.Replace("{$price}", post.Price.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
                               .Replace("{$currency}", post.Currency.ToString())
                               .Replace("{$price-display}", "block")
                               .Replace("{$price-comment-display}", "none")
                               .Replace("{$price-comment}", string.Empty);
        }
        else
        {
            postHtml = postHtml.Replace("{$price-comment}", post.PriceComment ?? "-")
                               .Replace("{$price-comment-display}", "block")
                               .Replace("{$price-display}", "none")
                               .Replace("{$price}", string.Empty)
                               .Replace("{$currency}", string.Empty);
        }

        // additional fees
        if (post.AdditionalFees is not null and not decimal.Zero)
        {
            postHtml = postHtml.Replace("{$additional-fees}", post.AdditionalFees.Value.ToString("N", new NumberFormatInfo { NumberGroupSeparator = " " }))
                               .Replace("{$additional-fees-display}", "inline-block");
        }
        else
        {
            postHtml = postHtml.Replace("{$additional-fees}", decimal.Zero.ToString("N"))
                               .Replace("{$additional-fees-display}", "none");
        }

        // text
        if (!string.IsNullOrEmpty(post.Text))
        {
            postHtml = postHtml.Replace("{$text}", post.Text)
                               .Replace("{$text-display}", "table");
        }
        else
        {
            postHtml = postHtml.Replace("{$text}", string.Empty)
                               .Replace("{$text-display}", "none");
        }

        return postHtml;
    }
}