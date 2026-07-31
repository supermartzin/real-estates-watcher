using System.Net;
using MimeKit;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Email;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class EmailNotifyingAdPostsHandlerTests
{
    [Fact]
    public void Constructor_RejectsNullSettings() =>
        Assert.Throws<ArgumentNullException>(() => new EmailNotifyingAdPostsHandler(null!));

    [Fact]
    public async Task NewPost_RejectsNullInput()
    {
        var handler = new EmailNotifyingAdPostsHandler(new EmailNotifyingAdPostsHandlerSettings());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleNewRealEstateAdPostAsync(null!));
    }

    [Fact]
    public async Task NewPosts_RejectsNullInput()
    {
        var handler = new EmailNotifyingAdPostsHandler(new EmailNotifyingAdPostsHandlerSettings());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleNewRealEstatesAdPostsAsync(null!));
    }

    [Fact]
    public async Task MissingSettings_FailBeforeAnyNetworkConnection()
    {
        var handler = new EmailNotifyingAdPostsHandler(new EmailNotifyingAdPostsHandlerSettings());

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost()));

        Assert.Contains("From address", exception.Message);
    }

    [Theory]
    [InlineData("FromAddress", "From address")]
    [InlineData("SenderName", "Sender name")]
    [InlineData("Username", "Username")]
    [InlineData("Password", "Password")]
    [InlineData("SmtpServerHost", "SMTP server host")]
    [InlineData("SmtpServerPort", "SMTP server port")]
    public async Task EveryRequiredSetting_IsValidated(string missingSetting, string expectedMessage)
    {
        var completeSettings = CreateCompleteSettings();
        var settings = completeSettings with
        {
            FromAddress = missingSetting == "FromAddress" ? null : completeSettings.FromAddress,
            SenderName = missingSetting == "SenderName" ? null : completeSettings.SenderName,
            Username = missingSetting == "Username" ? null : completeSettings.Username,
            Password = missingSetting == "Password" ? null : completeSettings.Password,
            SmtpServerHost = missingSetting == "SmtpServerHost" ? null : completeSettings.SmtpServerHost,
            SmtpServerPort = missingSetting == "SmtpServerPort" ? null : completeSettings.SmtpServerPort
        };
        var handler = new EmailNotifyingAdPostsHandler(settings);

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost()));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task CancelledSend_WrapsCancellationAfterBuildingCompleteMessage()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var sender = new MockSmtpEmailSender { Exception = new OperationCanceledException(cancellation.Token) };
        var handler = new EmailNotifyingAdPostsHandler(CreateCompleteSettings(), emailSender: sender);

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleNewRealEstatesAdPostsAsync([TestData.CreatePost()], cancellation.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
        Assert.Equal(cancellation.Token, sender.CancellationToken);
    }

    [Fact]
    public async Task NewPost_BuildsMessageAndPassesConnectionSettingsToSender()
    {
        var settings = CreateCompleteSettings();
        var sender = new MockSmtpEmailSender();
        var handler = new EmailNotifyingAdPostsHandler(settings, emailSender: sender);
        var post = TestData.CreatePost("new-home");

        await handler.HandleNewRealEstateAdPostAsync(post);

        var message = Assert.IsType<MimeMessage>(sender.Message);
        Assert.Equal("🆕 Nový inzerát nehnuteľnosti!", message.Subject);
        Assert.Equal("sender@example.test", Assert.Single(message.From.Mailboxes).Address);
        Assert.Equal("recipient@example.test", Assert.Single(message.To.Mailboxes).Address);
        Assert.Equal("copy@example.test", Assert.Single(message.Cc.Mailboxes).Address);
        Assert.Equal("hidden@example.test", Assert.Single(message.Bcc.Mailboxes).Address);
        Assert.Contains(post.WebUrl.AbsoluteUri, message.HtmlBody);
        Assert.Equal(settings.SmtpServerHost, sender.Host);
        Assert.Equal(settings.SmtpServerPort, sender.Port);
        Assert.True(sender.UseSecureConnection);
        Assert.Equal(settings.Username, sender.Credentials?.UserName);
        Assert.Equal(settings.Password, sender.Credentials?.Password);
    }

    [Fact]
    public async Task NewPosts_UsesPluralSubjectAndDefaultsSecureConnectionToTrue()
    {
        var sender = new MockSmtpEmailSender();
        var settings = CreateCompleteSettings() with { UseSecureConnection = null };
        var handler = new EmailNotifyingAdPostsHandler(settings, emailSender: sender);

        await handler.HandleNewRealEstatesAdPostsAsync([
            TestData.CreatePost("first"),
            TestData.CreatePost("second")
        ]);

        Assert.Equal("🆕 Nové inzeráty nehnuteľností!", sender.Message?.Subject);
        Assert.True(sender.UseSecureConnection);
        Assert.Contains("first", sender.Message?.HtmlBody);
        Assert.Contains("second", sender.Message?.HtmlBody);
    }

    [Fact]
    public async Task InitialNotification_UsesInitialSubjectWhenNotSkipped()
    {
        var sender = new MockSmtpEmailSender();
        var handler = new EmailNotifyingAdPostsHandler(CreateCompleteSettings(), emailSender: sender);

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost()]);

        Assert.Equal("🏦 Vaše aktuálne ponuky nehnuteľností", sender.Message?.Subject);
    }

    [Fact]
    public async Task SenderFailure_IsWrappedAndPreservesCause()
    {
        var cause = new IOException("connection lost");
        var sender = new MockSmtpEmailSender { Exception = cause };
        var handler = new EmailNotifyingAdPostsHandler(CreateCompleteSettings(), emailSender: sender);

        var exception = await Assert.ThrowsAsync<RealEstateAdPostsHandlerException>(
            () => handler.HandleNewRealEstateAdPostAsync(TestData.CreatePost()));

        Assert.Same(cause, exception.InnerException);
        Assert.Contains("connection lost", exception.Message);
    }

    [Fact]
    public async Task InitialNotification_CanBeSkippedWithoutSmtpConfiguration()
    {
        var handler = new EmailNotifyingAdPostsHandler(new EmailNotifyingAdPostsHandlerSettings
        {
            SkipInitialNotification = true
        });

        await handler.HandleInitialRealEstateAdPostsAsync([TestData.CreatePost()]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsEnabled_ReflectsSettings(bool enabled)
    {
        var handler = new EmailNotifyingAdPostsHandler(new EmailNotifyingAdPostsHandlerSettings { Enabled = enabled });

        Assert.Equal(enabled, handler.IsEnabled);
    }

    private static EmailNotifyingAdPostsHandlerSettings CreateCompleteSettings() => new()
    {
        Enabled = true,
        FromAddress = "sender@example.test",
        SenderName = "Sender",
        ToAddresses = ["recipient@example.test"],
        CcAddresses = ["copy@example.test"],
        BccAddresses = ["hidden@example.test"],
        SmtpServerHost = "localhost",
        SmtpServerPort = 1,
        UseSecureConnection = true,
        Username = "username",
        Password = "password"
    };

    private sealed class MockSmtpEmailSender : ISmtpEmailSender
    {
        public MimeMessage? Message { get; private set; }
        public string? Host { get; private set; }
        public int Port { get; private set; }
        public bool UseSecureConnection { get; private set; }
        public NetworkCredential? Credentials { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public Exception? Exception { get; init; }

        public Task SendAsync(
            MimeMessage message,
            string host,
            int port,
            bool useSecureConnection,
            NetworkCredential credentials,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            Host = host;
            Port = port;
            UseSecureConnection = useSecureConnection;
            Credentials = credentials;
            CancellationToken = cancellationToken;

            return Exception is null ? Task.CompletedTask : Task.FromException(Exception);
        }
    }
}

public class RealEstateAdPostsHandlerExceptionTests
{
    [Fact]
    public void Constructors_PreserveMessagesAndInnerExceptions()
    {
        var inner = new InvalidOperationException("inner");

        Assert.Null(new RealEstateAdPostsHandlerException().InnerException);
        Assert.Equal("message", new RealEstateAdPostsHandlerException("message").Message);
        Assert.Same(inner, new RealEstateAdPostsHandlerException("message", inner).InnerException);
    }
}
