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
    try {
        const browser = await puppeteer.launch({
            ignoreDefaultArgs: ['--disable-extensions --user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'],
            headless: false
        });
    
        const page = await browser.newPage();

        const cookies = parseCookies(process.argv[4]);
        if (cookies) {
            await page.setCookie(...cookies);
        }

        await page.goto(process.argv[3]).then(async () => {
            // wait for the specified time before closing the browser
            setTimeout(async () => {
                console.log(await page.content());

                await browser.close();
            }, process.argv[2] * 1000);
        }).catch(async (err) => {
            console.error(err);

            await browser.close();
        });
    } catch (err) {
        console.error(err);
    }
})();