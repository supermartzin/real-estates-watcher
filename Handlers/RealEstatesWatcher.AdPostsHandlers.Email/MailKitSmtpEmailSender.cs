using System.Net;
using MailKit.Net.Smtp;
using MimeKit;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

public sealed class MailKitSmtpEmailSender : ISmtpEmailSender
{
    public async Task SendAsync(
        MimeMessage message,
        string host,
        int port,
        bool useSecureConnection,
        NetworkCredential credentials,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(host, port, useSecureConnection, cancellationToken).ConfigureAwait(false);
        await client.AuthenticateAsync(credentials, cancellationToken).ConfigureAwait(false);
        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
