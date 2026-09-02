using System.Diagnostics;
using System.Text;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Tests;

public class LocalNodejsConsoleWebScraperTests
{
    private static readonly string NodeExecutablePath = OperatingSystem.IsWindows()
        ? @"C:\Program Files\nodejs\node.exe"
        : "/usr/local/bin/node";

    [Fact]
    public void Constructor_RejectsNullSettings() =>
        Assert.Throws<ArgumentNullException>(() => new LocalNodejsConsoleWebScraper(null!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StringOverload_RejectsMissingUrl(string? url)
    {
        var scraper = CreateScraper();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => scraper.GetFullWebPageContentAsync(url!));
    }

    [Fact]
    public async Task UriOverload_RejectsNullUri()
    {
        var scraper = CreateScraper();

        await Assert.ThrowsAsync<ArgumentNullException>(() => scraper.GetFullWebPageContentAsync((Uri)null!));
    }

    [Fact]
    public async Task NegativeTimeout_IsRejectedBeforeStartingAProcess()
    {
        var runner = new MockProcessRunner(new ProcessExecutionResult(0, "<html></html>", string.Empty));
        var scraper = CreateScraper(timeout: -1, runner: runner);

        var exception = await Assert.ThrowsAsync<WebScraperException>(
            () => scraper.GetFullWebPageContentAsync(new Uri("https://example.test")));

        Assert.Contains("invalid value", exception.Message);
        Assert.Equal(0, runner.CallCount);
    }

    [Theory]
    [InlineData("not a URL")]
    [InlineData("   ")]
    public async Task StringOverload_RejectsMalformedUrls(string url)
    {
        var scraper = CreateScraper();

        await Assert.ThrowsAsync<UriFormatException>(() => scraper.GetFullWebPageContentAsync(url));
    }

    [Fact]
    public async Task SuccessfulRun_UsesSeparatedArgumentsAndExtractsOnlyHtml()
    {
        var runner = new MockProcessRunner(new ProcessExecutionResult(
            0,
            "diagnostic output\n<HTML lang=\"en\"><body>page</body></HTML>\nfinished",
            string.Empty));
        var settings = new LocalNodejsConsoleWebScraperSettings
        {
            PathToNodeExecutable = NodeExecutablePath,
            PathToScript = @"C:\scripts with spaces\scraper.js",
            PathToCookiesFile = @"C:\cookies with spaces\cookies.json",
            PageScrapingTimeoutSeconds = 7
        };
        var scraper = new LocalNodejsConsoleWebScraper(settings, processRunner: runner);
        var encoding = Encoding.Unicode;
        using var cancellation = new CancellationTokenSource();
        var uri = new Uri("https://example.test/search?value=%22%26whoami");

        var result = await scraper.GetFullWebPageContentAsync(uri, encoding, cancellation.Token);

        Assert.Equal("<HTML lang=\"en\"><body>page</body></HTML>", result);
        Assert.NotNull(runner.StartInfo);
        Assert.Equal(settings.PathToNodeExecutable, runner.StartInfo.FileName);
        Assert.Equal(
            [settings.PathToScript, "7", uri.AbsoluteUri, settings.PathToCookiesFile],
            runner.StartInfo.ArgumentList.ToArray());
        Assert.Same(encoding, runner.OutputEncoding);
        Assert.Equal(TimeSpan.FromSeconds(10), runner.Timeout);
        Assert.Equal(cancellation.Token, runner.CancellationToken);
    }

    [Fact]
    public void Constructor_RejectsRelativeNodeExecutablePath()
    {
        var settings = new LocalNodejsConsoleWebScraperSettings
        {
            PathToNodeExecutable = "node",
            PathToScript = "unused.js",
            PageScrapingTimeoutSeconds = 1
        };

        var exception = Assert.Throws<ArgumentException>(() => new LocalNodejsConsoleWebScraper(settings));

        Assert.Contains("must be absolute", exception.Message);
    }

    [Fact]
    public async Task RunWithoutCookies_DoesNotAddAnEmptyArgument()
    {
        var runner = new MockProcessRunner(new ProcessExecutionResult(0, "<html></html>", string.Empty));
        var scraper = CreateScraper(runner: runner);

        await scraper.GetFullWebPageContentAsync(new Uri("https://example.test"));

        Assert.Equal(3, runner.StartInfo!.ArgumentList.Count);
        Assert.Same(Encoding.UTF8, runner.OutputEncoding);
    }

    [Theory]
    [InlineData(1, "", "Node.js process exited with code 1")]
    [InlineData(0, "script failed", "script failed")]
    public async Task ProcessFailure_IsReportedAsWebScraperException(int exitCode, string standardError, string expectedMessage)
    {
        var runner = new MockProcessRunner(new ProcessExecutionResult(exitCode, string.Empty, standardError));
        var scraper = CreateScraper(runner: runner);

        var exception = await Assert.ThrowsAsync<WebScraperException>(
            () => scraper.GetFullWebPageContentAsync(new Uri("https://example.test")));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("plain output")]
    [InlineData("</html>")]
    [InlineData("<html>without closing tag")]
    public async Task OutputWithoutCompleteHtmlDocument_IsRejected(string output)
    {
        var runner = new MockProcessRunner(new ProcessExecutionResult(0, output, string.Empty));
        var scraper = CreateScraper(runner: runner);

        var exception = await Assert.ThrowsAsync<WebScraperException>(
            () => scraper.GetFullWebPageContentAsync(new Uri("https://example.test")));

        Assert.Contains("No web page content", exception.Message);
    }

    [Fact]
    public async Task UnexpectedRunnerFailure_IsWrappedAndPreservesCause()
    {
        var cause = new TimeoutException("too slow");
        var runner = new MockProcessRunner(cause);
        var scraper = CreateScraper(runner: runner);

        var exception = await Assert.ThrowsAsync<WebScraperException>(
            () => scraper.GetFullWebPageContentAsync(new Uri("https://example.test")));

        Assert.Same(cause, exception.InnerException);
        Assert.Contains("too slow", exception.Message);
    }

    private static LocalNodejsConsoleWebScraper CreateScraper(int timeout = 1, IProcessRunner? runner = null) => new(
        new LocalNodejsConsoleWebScraperSettings
        {
            PathToNodeExecutable = NodeExecutablePath,
            PathToScript = "unused.js",
            PageScrapingTimeoutSeconds = timeout
        },
        processRunner: runner);

    private sealed class MockProcessRunner : IProcessRunner
    {
        private readonly ProcessExecutionResult? _result;
        private readonly Exception? _exception;

        public MockProcessRunner(ProcessExecutionResult result) => _result = result;

        public MockProcessRunner(Exception exception) => _exception = exception;

        public int CallCount { get; private set; }
        public ProcessStartInfo? StartInfo { get; private set; }
        public Encoding? OutputEncoding { get; private set; }
        public TimeSpan Timeout { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ProcessExecutionResult> RunAsync(
            ProcessStartInfo startInfo,
            Encoding outputEncoding,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            StartInfo = startInfo;
            OutputEncoding = outputEncoding;
            Timeout = timeout;
            CancellationToken = cancellationToken;

            return _exception is null
                ? Task.FromResult(_result!)
                : Task.FromException<ProcessExecutionResult>(_exception);
        }
    }
}

public class SystemProcessRunnerTests
{
    private readonly SystemProcessRunner _runner = new();

    [Fact]
    public async Task Run_RejectsNullStartInfo() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _runner.RunAsync(null!, Encoding.UTF8, TimeSpan.FromSeconds(1)));

    [Fact]
    public async Task Run_RejectsNullEncoding() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _runner.RunAsync(new ProcessStartInfo(), null!, TimeSpan.FromSeconds(1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Run_RejectsNonPositiveTimeout(int seconds) =>
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _runner.RunAsync(new ProcessStartInfo(), Encoding.UTF8, TimeSpan.FromSeconds(seconds)));
}

public class WebScraperExceptionTests
{
    [Fact]
    public void Constructors_PreserveMessagesAndInnerExceptions()
    {
        var inner = new InvalidOperationException("inner");

        Assert.Null(new WebScraperException().InnerException);
        Assert.Equal("message", new WebScraperException("message").Message);
        Assert.Same(inner, new WebScraperException("message", inner).InnerException);
    }
}
