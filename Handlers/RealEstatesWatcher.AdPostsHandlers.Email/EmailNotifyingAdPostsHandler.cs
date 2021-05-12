using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Email
{
    public class EmailNotifyingAdPostsHandler : IRealEstateAdPostsHandler
    {
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

            await SendEmail("New Real Estate Ad published!",
                            $"Received new Real Estate Ad Post: {adPost}{Environment.NewLine}Link: {adPost.WebUrl.AbsolutePath}").ConfigureAwait(false);
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

            await SendEmail("Initial Real Estate Ads report",
                            $"Received initial {adPosts.Count} Real Estate Ad Posts.{Environment.NewLine + Environment.NewLine}Links:{Environment.NewLine} • {string.Join($"{Environment.NewLine} • ", adPosts.Select(post => post.WebUrl.AbsoluteUri))}").ConfigureAwait(false);
        }

        private async Task SendEmail(string subject, string body)
        {
            var message = new MimeMessage
            {
                Subject = subject,
                Body = new TextPart(TextFormat.Plain)
                {
                    Text = body
                },
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
    }
}