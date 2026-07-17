#!/usr/bin/env node
import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();
const loginResponses = [];
page.on('response', async (r) => {
  if (r.url().includes('/api/oauth/Login')) {
    let body = '';
    try { body = (await r.text()).slice(0, 500); } catch { /* */ }
    loginResponses.push({ status: r.status(), body });
  }
});

await page.goto('http://localhost:3100/login', { waitUntil: 'domcontentloaded' });
await page.locator('input[placeholder*="账号"], input[placeholder*="账户"]').first().fill('admin');
await page.fill('input[type="password"]', '123456');
await page.getByRole('button', { name: /登.*录/ }).click();

try {
  await page.waitForURL(/#\/(home|workStation|dashboard|studio)/, { timeout: 15000 });
} catch {
  // keep going
}

await page.waitForTimeout(2000);
const info = {
  url: page.url(),
  hash: await page.evaluate(() => location.hash),
  loginResponses,
  tokenInStorage: await page.evaluate(() => {
    const raw = localStorage.getItem('COMMON__LOCAL__KEY__');
    if (!raw) return null;
    try {
      const c = JSON.parse(raw);
      return c?.TOKEN__?.value ? `${String(c.TOKEN__.value).slice(0, 40)}…` : null;
    } catch { return 'parse-error'; }
  }),
};
console.log(JSON.stringify(info, null, 2));
await page.screenshot({ path: '.claude/evidence/login-attempt.png', fullPage: true });

await page.goto('http://localhost:3100/studio/ai/submit-requirement', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(3000);
const textareaCount = await page.getByTestId('submit-requirement-textarea').count();
console.log('submit-requirement textarea count:', textareaCount);
await page.screenshot({ path: '.claude/evidence/submit-req-after-login.png', fullPage: true });

await browser.close();
