const puppeteer = require("puppeteer");
const fs = require('fs');

const pathToCookiesFile = "./cookies.json";
const defaultFileEncoding = "utf8";

function parseCookies() {
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
            ignoreDefaultArgs: ['--disable-extensions'],
            headless: true
        });
    
        const page = await browser.newPage();

        const cookies = parseCookies();
        if (cookies) {
            await page.setCookie(...cookies);
        }

        await page.goto(process.argv[2], {
            waitUntil: "networkidle0",
            timeout: 20000
        }).then(async () => {
            const data = await page.content();
            await browser.close();

            console.log(data);
        }).catch(async (err) => {
            console.error(err);

            await browser.close();
        });
    } catch (err) {
        console.error(err);
    }
})();