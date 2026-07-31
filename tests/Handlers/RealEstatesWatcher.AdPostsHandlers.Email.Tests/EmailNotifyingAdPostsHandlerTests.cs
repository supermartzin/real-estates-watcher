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
}
