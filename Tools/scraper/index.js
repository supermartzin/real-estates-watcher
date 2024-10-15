import { launch } from "puppeteer";

(async function () {
    try {
        const browser = await launch({
            ignoreDefaultArgs: ['--disable-extensions'],
            headless: true
        });
        const page = await browser.newPage();

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