const puppeteer = require("puppeteer");

(async function () {
    try {
        const browser = await puppeteer.launch({ ignoreDefaultArgs: ['--disable-extensions'], headless: true });
        const page = await browser.newPage();

        await page.goto(process.argv[2], { waitUntil: "networkidle0" });
        const data = await page.content();

        await browser.close();

        console.log(data);
    } catch (err) {
        console.error(err);
    }
})();