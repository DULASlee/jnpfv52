import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const BASE = 'https://zj.ysbz.119.gov.cn';
const USER = '11120002';
const PASS = 'xiaofang1#';
const OUT = path.join(root, '.claude/evidence/zj-recipe-api-report.json');
const PROFILE = path.join(root, '.claude/evidence/zj-browser-profile');

const apiLog = [];
function isRecipeRelated(url, body) {
  return /shipu|recipe|食谱|菜谱|canteen|food|menu|dish|meal|配餐|餐饮/i.test(url + (body || ''));
}

async function waitForRealLogin(page, maxSec = 120) {
  for (let i = 0; i < maxSec; i++) {
    const title = await page.title();
    const hasPass = (await page.locator('input[type="password"]').count()) > 0;
    const blocked = title.includes('NWAF') || title.includes('请稍候');
    if (i % 10 === 0) console.log(`[${i}s] title=${title} passwordField=${hasPass}`);
    if (hasPass && !blocked) return true;
    if (blocked && i === 0) console.log('>>> 请在弹出的 Chrome 窗口内完成 WAF/人机验证（如有），脚本最多等 120 秒');
    await page.waitForTimeout(1000);
  }
  return (await page.locator('input[type="password"]').count()) > 0;
}

async function main() {
  fs.mkdirSync(PROFILE, { recursive: true });
  fs.mkdirSync(path.dirname(OUT), { recursive: true });

  const context = await chromium.launchPersistentContext(PROFILE, {
    headless: false,
    channel: 'chrome',
    slowMo: 50,
    args: ['--disable-blink-features=AutomationControlled', '--start-maximized'],
    viewport: null,
    ignoreHTTPSErrors: true,
  });
  const page = context.pages()[0] || (await context.newPage());
  await page.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => false }); });

  page.on('response', async (res) => {
    const url = res.url();
    if (!url.startsWith('http') || /\.(js|css|png|jpg|gif|svg|woff|ico|map)(\?|$)/i.test(url)) return;
    const entry = { status: res.status(), method: res.request().method(), url };
    try {
      const ct = res.headers()['content-type'] || '';
      entry.contentType = ct;
      if (ct.includes('json') || /api|oauth|blade|admin|shipu|recipe/i.test(url)) {
        const text = await res.text();
        entry.rawPreview = text.slice(0, 1500);
        try { entry.json = JSON.parse(text); entry.parseOk = true; } catch { entry.parseOk = false; }
        entry.recipeRelated = isRecipeRelated(url, text);
        apiLog.push(entry);
        console.log(`[NET] ${entry.status} json=${entry.parseOk} recipe=${entry.recipeRelated} ${url.slice(0, 120)}`);
      }
    } catch (e) { entry.error = e.message; apiLog.push(entry); }
  });

  console.log('打开登录页...');
  await page.goto(`${BASE}/#/login`, { waitUntil: 'domcontentloaded', timeout: 120000 });
  const ready = await waitForRealLogin(page, 120);

  if (!ready) {
    console.log('未检测到登录框，当前可能被 WAF 拦截');
  } else {
    console.log('自动填写并登录...');
    await page.locator('input[type="text"], input:not([type="password"]):not([type="hidden"])').first().fill(USER);
    await page.locator('input[type="password"]').first().fill(PASS);
    const btn = page.locator('button').filter({ hasText: /登\s*录|登录/i }).first();
    if (await btn.count()) await btn.click(); else await page.keyboard.press('Enter');
    await page.waitForTimeout(12000);
  }

  console.log('URL:', page.url(), 'title:', await page.title());

  for (const t of ['食谱', '菜谱', '餐饮', '配餐', '伙食', '菜单']) {
    const loc = page.getByText(t, { exact: false }).first();
    if (await loc.count()) {
      console.log('点击:', t);
      await loc.click({ timeout: 8000 }).catch(() => {});
      await page.waitForTimeout(6000);
    }
  }

  await page.waitForTimeout(10000);
  await page.screenshot({ path: path.join(root, '.claude/evidence/zj-local-final.png'), fullPage: true }).catch(() => {});

  const recipeApis = apiLog.filter((a) => a.recipeRelated);
  const report = {
    testedAt: new Date().toISOString(),
    finalUrl: page.url(),
    pageTitle: await page.title(),
    loginDetected: ready,
    totalApiCaptured: apiLog.length,
    parsedOkCount: apiLog.filter((a) => a.parseOk).length,
    recipeRelatedCount: recipeApis.length,
    recipeApis,
    allApisPreview: apiLog.slice(0, 30).map(({ url, status, parseOk, recipeRelated, json, rawPreview }) => ({
      url, status, parseOk, recipeRelated,
      jsonKeys: json && typeof json === 'object' ? Object.keys(json).slice(0, 20) : null,
      preview: rawPreview?.slice(0, 300),
    })),
  };
  fs.writeFileSync(OUT, JSON.stringify(report, null, 2));
  console.log('\n===== SUMMARY =====');
  console.log(JSON.stringify({ ...report, recipeApis: recipeApis.slice(0, 3), allApisPreview: report.allApisPreview.slice(0, 10) }, null, 2));
  console.log('Full report:', OUT);
  await context.close();
}

main().catch((e) => { console.error(e); process.exit(1); });
