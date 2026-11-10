using RealEstatesWatcher.AdPostsFilters.BasicFilter;
using RealEstatesWatcher.AdPostsHandlers.File;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.UI.Console;

namespace RealEstatesWatcher.Tests;

public class WatcherConfigurationTests : IDisposable
{
    private readonly string _configPath = Path.Combine(Path.GetTempPath(), $"watcher-{Guid.NewGuid():N}.ini");

    [Fact]
    public void Load_MapsAllSettingsFromOneFile()
    {
        var settings = Load("""
            [general]
            check_interval_minutes=15
            start_periodic_check_at=07:30:00
            perform_check_on_startup=false
            enable_multiple_portal_instances=false
            [gcloud]
            enable_cloud_logging=true
            application_id=test-app
            project_id=test-project
            service_name=test-service
            [email]
            enabled=true
            from=watcher@example.test
            to= first@example.test, second@example.test ,,
            cc=copy@example.test
            bcc=hidden@example.test
            sender_name=Watcher
            username=test-user
            password=dummy-password
            smtp_server_host=smtp.example.test
            smtp_server_port=587
            use_secure_connection=true
            skip_initial_notification=true
            [file]
            enabled=true
            main_path=ads.txt
            separate_new_posts=true
            new_posts_path=new-ads.txt
            print_format=HTML
            [basic]
            price_min=1000000
            price_max=5000000
            layouts=2+kk,3+1
            floor_area_min=40
            floor_area_max=100
            [nodejs]
            path_to_node_executable=/usr/bin/node
            path_to_script=scraper/index.js
            page_scraping_timeout_seconds=60
            path_to_cookies_file=scraper/cookies.json
            [portals]
            0=https://reality.bazos.cz/prodam/byt/?price=5000000&area=100
            1=https://www.sreality.cz/hledani/prodej/byty
            """);

        Assert.Equal(15, settings.Engine.CheckIntervalMinutes);
        Assert.Equal(new TimeOnly(7, 30), settings.Engine.StartCheckAtSpecificTime);
        Assert.False(settings.Engine.PerformCheckOnStartup);
        Assert.False(settings.Engine.EnableMultiplePortalInstances);
        Assert.True(settings.GoogleCloud.EnableCloudLogging);
        Assert.Equal("test-app", settings.GoogleCloud.ApplicationId);
        Assert.Equal("test-project", settings.GoogleCloud.ProjectId);
        Assert.Equal("test-service", settings.GoogleCloud.ServiceName);
        Assert.True(settings.EmailHandler.Enabled);
        Assert.Equal("watcher@example.test", settings.EmailHandler.FromAddress);
        Assert.Equal(["first@example.test", "second@example.test"], settings.EmailHandler.ToAddresses);
        Assert.Equal(["copy@example.test"], settings.EmailHandler.CcAddresses);
        Assert.Equal(["hidden@example.test"], settings.EmailHandler.BccAddresses);
        Assert.Equal("Watcher", settings.EmailHandler.SenderName);
        Assert.Equal("test-user", settings.EmailHandler.Username);
        Assert.Equal("dummy-password", settings.EmailHandler.Password);
        Assert.Equal("smtp.example.test", settings.EmailHandler.SmtpServerHost);
        Assert.Equal(587, settings.EmailHandler.SmtpServerPort);
        Assert.True(settings.EmailHandler.UseSecureConnection);
        Assert.True(settings.EmailHandler.SkipInitialNotification);
        Assert.True(settings.FileHandler.Enabled);
        Assert.Equal("ads.txt", settings.FileHandler.MainFilePath);
        Assert.True(settings.FileHandler.NewPostsToSeparateFile);
        Assert.Equal("new-ads.txt", settings.FileHandler.NewPostsFilePath);
        Assert.Equal(PrintFormat.Html, settings.FileHandler.PrintFormat);
        var filter = Assert.IsType<BasicParametersAdPostsFilterSettings>(settings.BasicFilter);
        Assert.Equal(1000000m, filter.MinPrice);
        Assert.Equal(5000000m, filter.MaxPrice);
        Assert.Equal(40m, filter.MinFloorArea);
        Assert.Equal(100m, filter.MaxFloorArea);
        Assert.True(filter.Layouts.SetEquals([Layout.TwoPlusKk, Layout.ThreePlusOne, Layout.NotSpecified]));
        var scraper = Assert.IsType<LocalNodejsConsoleWebScraperSettings>(settings.WebScraper);
        Assert.Equal("/usr/bin/node", scraper.PathToNodeExecutable);
        Assert.Equal("scraper/index.js", scraper.PathToScript);
        Assert.Equal(60, scraper.PageScrapingTimeoutSeconds);
        Assert.Equal("scraper/cookies.json", scraper.PathToCookiesFile);
        Assert.Equal([
            "https://reality.bazos.cz/prodam/byt/?price=5000000&area=100",
            "https://www.sreality.cz/hledani/prodej/byty"
        ], settings.PortalUrls);
    }

    [Fact]
    public void Load_PreservesDefaultsWhenOptionalSettingsAreAbsent()
    {
        var settings = Load("""
            [general]
            check_interval_minutes=10
            [portals]
            0=https://reality.bazos.cz/prodam/byt/
            """);

        Assert.True(settings.Engine.PerformCheckOnStartup);
        Assert.True(settings.Engine.EnableMultiplePortalInstances);
        Assert.Null(settings.Engine.StartCheckAtSpecificTime);
        Assert.False(settings.GoogleCloud.EnableCloudLogging);
        Assert.False(settings.EmailHandler.Enabled);
        Assert.Empty(settings.EmailHandler.ToAddresses);
        Assert.Empty(settings.EmailHandler.CcAddresses);
        Assert.Empty(settings.EmailHandler.BccAddresses);
        Assert.False(settings.FileHandler.Enabled);
        Assert.Equal(PrintFormat.PlainText, settings.FileHandler.PrintFormat);
        Assert.Null(settings.BasicFilter);
        Assert.Null(settings.WebScraper);
    }

    [Fact]
    public void Load_AllowsEmptyNullableFilterValuesAndAnUnlimitedScraperTimeout()
    {
        var settings = Load("""
            [general]
            check_interval_minutes=10
            start_periodic_check_at=
            [basic]
            price_min=
            price_max=
            floor_area_min=
            floor_area_max=
            layouts=
            [nodejs]
            path_to_script=scraper/index.js
            page_scraping_timeout_seconds=0
            """);

        Assert.Null(settings.Engine.StartCheckAtSpecificTime);
        Assert.NotNull(settings.BasicFilter);
        Assert.Null(settings.BasicFilter.MinPrice);
        Assert.Null(settings.BasicFilter.MaxPrice);
        Assert.Null(settings.BasicFilter.MinFloorArea);
        Assert.Null(settings.BasicFilter.MaxFloorArea);
        Assert.Empty(settings.BasicFilter.Layouts);
        Assert.NotNull(settings.WebScraper);
        Assert.Null(settings.WebScraper.PathToNodeExecutable);
        Assert.Null(settings.WebScraper.PathToCookiesFile);
        Assert.Equal(0, settings.WebScraper.PageScrapingTimeoutSeconds);
    }

    [Fact]
    public void Load_IgnoresCommentedAndBlankPortalEntriesAndPreservesNumericOrder()
    {
        var settings = Load("""
            [portals]
            10=https://reality.bazos.cz/prodam/dum/
            ; 1=https://www.sreality.cz/hledani/prodej/byty
            2=   https://reality.idnes.cz/s/prodej/byty/
            3=
            """);

        Assert.Equal([
            "https://reality.idnes.cz/s/prodej/byty/",
            "https://reality.bazos.cz/prodam/dum/"
        ], settings.PortalUrls);
    }

    [Fact]
    public void Load_ResolvesRelativeConfigPathsFromTheWorkingDirectory()
    {
        File.WriteAllText(_configPath, "[general]\ncheck_interval_minutes=42");

        var settings = WatcherConfiguration.Load(Path.GetRelativePath(Environment.CurrentDirectory, _configPath));

        Assert.Equal(42, settings.Engine.CheckIntervalMinutes);
    }

    [Fact]
    public void Load_TemplateProvidesUsableDefaults()
    {
        var settings = WatcherConfiguration.Load(Path.Combine(AppContext.BaseDirectory, "watcher-template.ini"));

        Assert.Equal(10, settings.Engine.CheckIntervalMinutes);
        Assert.True(settings.Engine.PerformCheckOnStartup);
        Assert.True(settings.Engine.EnableMultiplePortalInstances);
        Assert.False(settings.GoogleCloud.EnableCloudLogging);
        Assert.False(settings.EmailHandler.Enabled);
        Assert.True(settings.FileHandler.Enabled);
        Assert.Equal("ads.txt", settings.FileHandler.MainFilePath);
        Assert.Equal(PrintFormat.PlainText, settings.FileHandler.PrintFormat);
        Assert.Equal(["https://reality.idnes.cz/s/prodej/byty/"], settings.PortalUrls);
        Assert.Null(settings.WebScraper);
    }

    [Fact]
    public void Load_ReportsMissingFiles()
    {
        Assert.Throws<FileNotFoundException>(() => WatcherConfiguration.Load(_configPath));
    }

    [Theory]
    [InlineData("[general]\ncheck_interval_minutes=not-a-number")]
    [InlineData("[general]\nperform_check_on_startup=not-a-boolean")]
    [InlineData("[nodejs]\npage_scraping_timeout_seconds=invalid")]
    public void Load_RejectsMalformedTypedSettings(string contents)
    {
        Assert.Throws<InvalidOperationException>(() => Load(contents));
    }

    private WatcherConfiguration Load(string contents)
    {
        File.WriteAllText(_configPath, contents);
        return WatcherConfiguration.Load(_configPath);
    }

    public void Dispose() => File.Delete(_configPath);
}
