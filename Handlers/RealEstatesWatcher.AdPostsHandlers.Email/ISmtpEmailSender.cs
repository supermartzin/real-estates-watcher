using System.Net;
using MimeKit;

namespace RealEstatesWatcher.AdPostsHandlers.Email;

public interface ISmtpEmailSender
{
    Task SendAsync(
        MimeMessage message,
        string host,
        int port,
        bool useSecureConnection,
        NetworkCredential credentials,
        CancellationToken cancellationToken = default);
}
