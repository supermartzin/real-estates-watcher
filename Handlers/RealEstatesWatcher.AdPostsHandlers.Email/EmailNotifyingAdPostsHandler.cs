using MailKit.Net.Smtp;

using Microsoft.Extensions.Logging;

using MimeKit;

using RealEstatesWatcher.AdPostsHandlers.Base.Html;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.Models;

using System.Globalization;
using System.Net;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

public class EmailNotifyingAdPostsHandler(EmailNotifyingAdPostsHandlerSettings settings,
                                          NumberFormatInfo? numberFormat = null,
                                          ILogger<EmailNotifyingAdPostsHandler>? logger = null)
    : HtmlBasedAdPostsHandlerBase(numberFormat ?? NumberFormatInfo.CurrentInfo), IRealEstateAdPostsHandler
{
    private readonly EmailNotifyingAdPostsHandlerSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public bool IsEnabled { get; } = settings.Enabled;

    public async Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPost);

        logger?.LogDebug("Received new Real Estate Ad Post: {Post}", adPost);

        await SendEmailAsync("🆕 New Real Estate Advert published!", CreateFullHtmlPage([adPost], CommonHtmlTemplateElements.TitleNewPosts), cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        logger?.LogDebug("Received '{PostsCount}' new Real Estate Ad Posts.", adPosts.Count);

        await SendEmailAsync("🆕 New Real Estate Adverts published!", CreateFullHtmlPage(adPosts, CommonHtmlTemplateElements.TitleNewPosts), cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adPosts);

        if (_settings.SkipInitialNotification is true)
        {
            logger?.LogDebug("Skipping initial notification on {PostsCount} Real Estate Ad posts", adPosts.Count);
            return;
        }

        logger?.LogDebug("Received initial {PostsCount} Real Estate Ad Posts.", adPosts.Count);

        await SendEmailAsync("🏦 Current Real Estate Adverts offering", CreateFullHtmlPage(adPosts, CommonHtmlTemplateElements.TitleInitialPosts), cancellationToken).ConfigureAwait(false);
    }

    private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        ThrowIfMissing(string.IsNullOrEmpty(_settings.FromAddress), "From address");
        ThrowIfMissing(string.IsNullOrEmpty(_settings.SenderName), "Sender name");
        ThrowIfMissing(string.IsNullOrEmpty(_settings.Username), "Username of sender's email server");
        ThrowIfMissing(string.IsNullOrEmpty(_settings.Password), "Password of sender's email server");
        ThrowIfMissing(string.IsNullOrEmpty(_settings.SmtpServerHost), "SMTP server host");
        ThrowIfMissing(_settings.SmtpServerPort is null, "SMTP server port");

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
                                      _settings.SmtpServerPort!.Value,
                                      _settings.UseSecureConnection ?? true,
                                      cancellationToken).ConfigureAwait(false);

            await client.AuthenticateAsync(new NetworkCredential(_settings.Username, _settings.Password), cancellationToken)
                .ConfigureAwait(false);

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


    private static void ThrowIfMissing(bool isMissing, string settingName)
    {
        if (isMissing)
            throw new RealEstateAdPostsHandlerException($"Email settings -> {settingName} is not specified in the configuration.");
    }
}