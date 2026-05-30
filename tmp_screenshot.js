const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  await page.goto('http://localhost:3100/index.html', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: 'D:/JNPF-v52/login-screenshot.png', fullPage: false });
  await browser.close();
  console.log('Screenshot saved');
})();
