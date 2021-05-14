using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Email
{
    public class EmailNotifyingAdPostsHandler : IRealEstateAdPostsHandler
    {
        private static class HtmlTemplates
        {
            public const string FullPage = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
</head>
<body style=""max-width: 800px; margin:10px auto;"">
    {$posts}
</body>
</html>";

            public const string Post = "<div style=\"padding: 10px; background: #ededed; min-height: 200px;\">\r\n    <div style=\"float: left; margin-right: 1em; width: 30%; height: 180px; display: {$img-display};\">\r\n        <img src=\"{$img-link}\" style=\"height: 100%; width: 100%; object-fit: cover;\" />\r\n    </div>\r\n    <a href=\"{$post-link}\">\r\n        <h3 style=\"margin: 0.2em;\">{$title}</h3>\r\n    </a>\r\n    <span style=\"font-size: medium; color: grey; display: {$price-display};\">\r\n        <b>{$price}</b> {$currency}<br/>\r\n    </span>\r\n    <span style=\"font-size: medium; color: grey; display: {$price-comment-display};\">\r\n        <b>{$price-comment}</b><br/>\r\n    </span>\r\n    <span>\r\n        <b>Server:</b> {$portal-name}<br/>\r\n        <b>Adresa:</b> {$address}<br/>\r\n        <b>Výmera:</b> {$floor-area}<br/>\r\n    </span>\r\n    <p style=\"margin: 0.2em; font-size: small; text-align: justify; display: {$text-display};\">{$text}</p>\r\n</div>";
        }

        private readonly ILogger<EmailNotifyingAdPostsHandler>? _logger;
        private readonly EmailNotifyingAdPostsHandlerSettings _settings;

        public EmailNotifyingAdPostsHandler(EmailNotifyingAdPostsHandlerSettings settings,
                                            ILogger<EmailNotifyingAdPostsHandler>? logger = default)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger;
        }

        public async Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost)
        {
            if (adPost == null)
                throw new ArgumentNullException(nameof(adPost));

            _logger?.LogDebug($"Received new Real Estate Ad Post: {adPost}");

            await SendEmail("New Real Estate Advert published!", CreateHtmlBody(adPost)).ConfigureAwait(false);
        }

        public async Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts)
        {
            if (adPosts == null)
                throw new ArgumentNullException(nameof(adPosts));

            _logger?.LogDebug($"Received '{adPosts.Count}' new Real Estate Ad Posts.");

            await SendEmail("New Real Estate Adverts published!", CreateHtmlBody(adPosts)).ConfigureAwait(false);
        }

        public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts)
        {
            if (adPosts == null)
                throw new ArgumentNullException(nameof(adPosts));
            if (_settings.SkipInitialNotification)
            {
                _logger?.LogDebug($"Skipping initial notification on {adPosts.Count} Real Estate Ad posts");
                return;
            }
            
            _logger?.LogDebug($"Received initial {adPosts.Count} Real Estate Ad Posts.");

            await SendEmail("Initial Real Estate Adverts list", CreateHtmlBody(adPosts)).ConfigureAwait(false);
        }

        private async Task SendEmail(string subject, string body)
        {
            var message = new MimeMessage
            {
                Subject = subject,
                Body = new BodyBuilder { HtmlBody = body }.ToMessageBody(),
                From =
                {
                    new MailboxAddress(_settings.SenderName, _settings.EmailAddressFrom)
                }
            };
            message.To.AddRange(_settings.EmailAddressesTo.Select(address => new MailboxAddress(address, address)));

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

                _logger?.LogInformation("Notification email has been successfully sent.");
            }
            catch (Exception ex)
            {
                throw new RealEstateAdPostsHandlerException($"Error during sending email notification: {ex.Message}", ex);
            }
        }

        private static string CreateHtmlBody(RealEstateAdPost adPost) => HtmlTemplates.FullPage.Replace("{$posts}", CreateSingleHtmlPost(adPost));

        private static string CreateHtmlBody(IEnumerable<RealEstateAdPost> adPosts) => HtmlTemplates.FullPage.Replace("{$posts}", string.Join(Environment.NewLine, adPosts.Select(CreateSingleHtmlPost)));

        private static string CreateSingleHtmlPost(RealEstateAdPost post)
        {
            var postHtml = HtmlTemplates.Post.Replace("{$title}", post.Title)
                                                   .Replace("{$portal-name}", post.AdsPortalName)
                                                   .Replace("{$post-link}", post.WebUrl.AbsoluteUri)
                                                   .Replace("{$address}", post.Address);

            // floor area
            if (post.FloorArea != decimal.Zero)
            {
                postHtml = postHtml.Replace("{$floor-area}", post.FloorArea + " m²");
            }
            else
            {
                postHtml = postHtml.Replace("{$floor-area}", " -");
            }
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
            if (post.Price != decimal.Zero)
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

            // text
            if (!string.IsNullOrEmpty(post.Text))
            {
                postHtml = postHtml.Replace("{$text}", post.Text)
                                   .Replace("{$text-display}", "block");
            }
            else
            {
                postHtml = postHtml.Replace("{$text-display}", "none");
            }

            return postHtml;
        }
    }
}