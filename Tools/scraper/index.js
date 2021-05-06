const puppeteer = require("puppeteer");

(async function () {
    try {
        const browser = await puppeteer.launch();
        const [page] = await browser.pages();

        await page.goto(process.argv[2], { waitUntil: "networkidle0" });
        const data = await page.evaluate(() => window.document.querySelector("*").outerHTML);

        console.log(data);

        await browser.close();
    } catch (err) {
        console.error(err);
    }
})();