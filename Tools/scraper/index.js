const puppeteer = require("puppeteer");
const fs = require('fs');

const defaultFileEncoding = "utf8";

function parseCookies(pathToCookiesFile) {
    try {
        if (fs.existsSync(pathToCookiesFile)) {
            let cookiesString = fs.readFileSync(pathToCookiesFile, defaultFileEncoding);

            return JSON.parse(cookiesString);
        }
    } catch (err) {
        return undefined;
    }
}

(async function () {
    const startTimeMs = Date.now();
    const waitSeconds = Number(process.argv[2]);
    const totalBudgetMs = Number.isFinite(waitSeconds) && waitSeconds > 0 ? waitSeconds * 1000 : 10000;
    const deadlineMs = startTimeMs + totalBudgetMs;

    let browser;
    try {
        browser = await puppeteer.launch({
            ignoreDefaultArgs: ['--disable-extensions'],
            args: ['--disable-features=NetworkServiceSandbox'],
            headless: false
        });

        const page = await browser.newPage();

        const cookies = parseCookies(process.argv[4]);
        if (cookies) {
            await page.setCookie(...cookies);
        }

        const gotoTimeoutMs = Math.max(1000, deadlineMs - Date.now());
        await page.goto(process.argv[3], { timeout: gotoTimeoutMs });

        // Wait within the remaining scraping budget for dynamic content to settle before closing.
        const settleDelayMs = Math.min(Math.max(0, deadlineMs - Date.now()), 5000);
        if (settleDelayMs > 0) {
            await new Promise((resolve) => setTimeout(resolve, settleDelayMs));
        }

        console.log(await page.content());
    } catch (err) {
        console.error(err);
        process.exitCode = 1;
    } finally {
        if (browser) {
            await browser.close();
        }
    }
})();