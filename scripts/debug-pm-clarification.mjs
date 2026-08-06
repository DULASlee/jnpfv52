#!/usr/bin/env node
// PM 澄清卡死调试：有头浏览器 + Network/Console 全抓取
// 用法：node scripts/debug-pm-clarification.mjs [pipelineId]
// 行为：自动登录 → 打开 pipeline → 监听 /clarification /answer + SSE + run，
//      你手动点「提交答案」，结束时按 Ctrl+C 生成报告
//
// 报告写入：.claude/evidence/pm-clarification-debug-{ts}.json

import { chromium } from 'playwright';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname } from 'node:path';

const PIPELINE_ID = process.argv[2] || '409';
const STUDIO_URL = `http://localhost:3100/#/studio/pipeline/${PIPELINE_ID}`;
const LOGIN_URL = 'http://localhost:3100/login';
const TS = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = `.claude/evidence/pm-clarification-debug-${TS}.json`;
mkdirSync(dirname(OUT), { recursive: true });

// 关注的请求路径片段
const WATCH = [
  '/clarification/',
  '/answer',
  '/requirement-analysis/',
  '/run',
  '/events',
  '/spec-content',
  '/refresh-spec',
];

const events = [];
const sseChunks = new Map(); // requestId -> {url, chunks:[]}

const browser = await chromium.launch({
  headless: false, // 有头模式，你能直接操作
  args: ['--start-maximized'],
});
const context = await browser.newContext({ viewport: null });
const page = await context.newPage();

// 启用 console
page.on('console', (msg) => {
  events.push({
    t: Date.now(),
    kind: 'console',
    type: msg.type(),
    text: msg.text().slice(0, 500),
  });
  if (msg.type() === 'error') {
    console.log(`[CONSOLE ERROR] ${msg.text().slice(0, 200)}`);
  }
});
page.on('pageerror', (err) => {
  events.push({ t: Date.now(), kind: 'pageerror', message: err.message, stack: err.stack?.slice(0, 800) });
  console.log(`[PAGE ERROR] ${err.message}`);
});

// 监听所有请求/响应
page.on('request', (req) => {
  const url = req.url();
  if (!WATCH.some((w) => url.includes(w))) return;
  if (req.resourceType() === 'xhr' || req.resourceType() === 'fetch' || req.resourceType() === 'eventsource') {
    events.push({
      t: Date.now(),
      kind: 'request',
      method: req.method(),
      url,
      postData: req.postData()?.slice(0, 1500),
    });
    console.log(`[${req.method()}] ${url.replace('http://localhost:5000', '')}${req.postData() ? '  body=' + req.postData().slice(0, 120) : ''}`);
  }
});

page.on('response', async (res) => {
  const url = res.url();
  const req = res.request();
  if (!WATCH.some((w) => url.includes(w))) return;

  // SSE：response 流，分段读取
  const ct = res.headers()['content-type'] || '';
  if (ct.includes('event-stream') || url.includes('/events')) {
    sseChunks.set(req.hashCode?.() || url + Date.now(), { url, chunks: [] });
    // 监听 requestfinished 拿不到 chunk，但能拿到结束
    return;
  }

  // 普通响应：读 body
  let body = '';
  let truncated = false;
  try {
    body = await res.text();
    if (body.length > 2000) {
      body = body.slice(0, 2000);
      truncated = true;
    }
  } catch (e) {
    body = `(body read error: ${e.message})`;
  }
  events.push({
    t: Date.now(),
    kind: 'response',
    method: req.method(),
    url: url.replace('http://localhost:5000', ''),
    status: res.status(),
    body,
    truncated,
  });
  console.log(`  → [${res.status()}] ${(body || '').slice(0, 200)}`);
});

// 登录
console.log(`\n=== PM 澄清卡死调试 (pipeline ${PIPELINE_ID}) ===\n`);
console.log('正在登录...');
await page.goto(LOGIN_URL, { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(1500);

try {
  await page.locator('input[placeholder*="账号"], input[placeholder*="账户"]').first().fill('admin', { timeout: 5000 });
  await page.fill('input[type="password"]', '123456');
  await page.getByRole('button', { name: /登.*录/ }).click();
  await page.waitForTimeout(2500);
} catch (e) {
  console.log('登录表单可能已登录或不可见:', e.message.slice(0, 100));
}

console.log(`\n打开 Studio pipeline ${PIPELINE_ID}...`);
await page.goto(STUDIO_URL, { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(3000);

console.log(`\n==================================================`);
console.log(`浏览器已打开 pipeline ${PIPELINE_ID}`);
console.log(`现在请你：`);
console.log(`  1. 在聊天里找到第 2 轮澄清题卡片`);
console.log(`  2. 作答后点「提交答案」`);
console.log(`  3. 观察是否卡住`);
console.log(`  4. 完成后回到此终端按 Ctrl+C 生成报告`);
console.log(`==================================================\n`);

// 每 10s 打印当前状态
const heartbeat = setInterval(() => {
  const recentCount = events.filter((e) => e.t > Date.now() - 10000).length;
  console.log(`[${new Date().toLocaleTimeString()}] 累计事件 ${events.length} 个，最近 10s ${recentCount} 个`);
}, 10000);

// 优雅退出
process.on('SIGINT', async () => {
  clearInterval(heartbeat);
  console.log('\n生成报告中...');

  // 抓当前 DOM 状态
  let domState = null;
  try {
    domState = await page.evaluate(() => {
      const msgs = Array.from(document.querySelectorAll('.chat-message, [class*="message"]')).slice(-5).map((el) => ({
        text: el.textContent?.slice(0, 300),
        cls: el.className,
      }));
      const clarification = document.querySelector('[class*="clarification"]');
      const thinking = Array.from(document.querySelectorAll('[class*="thinking"], [class*="reasoning"]'))
        .slice(-1).map((el) => el.textContent?.slice(0, 500));
      return {
        url: location.href,
        hash: location.hash,
        clarificationVisible: !!clarification,
        clarificationText: clarification?.textContent?.slice(0, 600),
        recentMessages: msgs,
        lastThinking: thinking,
      };
    });
  } catch (e) {
    domState = { error: e.message };
  }

  const report = {
    pipelineId: PIPELINE_ID,
    capturedAt: new Date().toISOString(),
    totalEvents: events.length,
    domState,
    events,
  };
  writeFileSync(OUT, JSON.stringify(report, null, 2));
  console.log(`\n✅ 报告已写入：${OUT}`);
  console.log(`\n关键事件摘要：`);

  // 打印关键请求
  const reqs = events.filter((e) => e.kind === 'request');
  const resps = events.filter((e) => e.kind === 'response');
  const errors = events.filter((e) => e.kind === 'pageerror' || (e.kind === 'console' && e.type === 'error'));
  console.log(`  请求数: ${reqs.length}`);
  console.log(`  响应数: ${resps.length}`);
  console.log(`  JS 错误: ${errors.length}`);
  if (errors.length) {
    console.log('\nJS 错误:');
    errors.slice(0, 5).forEach((e, i) => {
      console.log(`  ${i + 1}. ${e.message || e.text}`);
    });
  }

  await browser.close();
  process.exit(0);
});

// 保持进程运行
console.log('(调试会话运行中，按 Ctrl+C 结束并生成报告)');
await new Promise(() => {});
