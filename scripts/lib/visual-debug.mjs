#!/usr/bin/env node
/**
 * visual-debug — JNPF 可视化调试工具 (Motif 风格)
 *
 * 录制浏览器操作 GIF + 捕获 console/network 错误，让 AI Agent 逐帧分析 UI Bug。
 * 替代 "文字描述 → Agent 猜" 的低效模式。
 *
 * 用法:
 *   # 录制 10 秒 UI 操作
 *   node scripts/lib/visual-debug.mjs --url=http://localhost:3100/#/onlineDev/webDesign --duration=10
 *
 *   # 带登录录制
 *   node scripts/lib/visual-debug.mjs --login --url=http://localhost:3100/#/onlineDev/webDesign
 *
 *   # 录制移动端
 *   node scripts/lib/visual-debug.mjs --mobile --url=http://localhost:3800/#/pages/message/im/index?formUserId=10004
 *
 *   # 指定输出文件名
 *   node scripts/lib/visual-debug.mjs --url=... --output=my-bug
 *
 * 产出:
 *   .claude/evidence/visual-debug-<name>.gif      — UI 操作 GIF
 *   .claude/evidence/visual-debug-<name>.json     — console/network/WS 诊断数据
 *   .claude/evidence/visual-debug-<name>.png      — 最后一帧截图
 */

import { chromium } from 'playwright';
import { writeFileSync, mkdirSync, existsSync, readdirSync, renameSync, statSync, unlinkSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const EVIDENCE_DIR = resolve(__dirname, '..', '..', '.claude', 'evidence');

const args = process.argv.slice(2);
let url = '';
let duration = 15;
let login = false;
let mobile = false;
let output = '';

for (let i = 0; i < args.length; i++) {
  if (args[i] === '--url' || args[i] === '-u') url = args[++i];
  else if (args[i] === '--duration' || args[i] === '-d') duration = parseInt(args[++i]) || 15;
  else if (args[i] === '--login' || args[i] === '-l') login = true;
  else if (args[i] === '--mobile' || args[i] === '-m') mobile = true;
  else if (args[i] === '--output' || args[i] === '-o') output = args[++i];
}

if (!url) {
  console.log('用法: node scripts/lib/visual-debug.mjs --url=<URL> [--login] [--mobile] [--duration=15] [--output=name]');
  process.exit(1);
}

if (!existsSync(EVIDENCE_DIR)) mkdirSync(EVIDENCE_DIR, { recursive: true });

const timestamp = output || `debug-${Date.now()}`;
const gifPath = resolve(EVIDENCE_DIR, `visual-${timestamp}.gif`);
const pngPath = resolve(EVIDENCE_DIR, `visual-${timestamp}.png`);
const dataPath = resolve(EVIDENCE_DIR, `visual-${timestamp}.json`);

// ── 诊断数据收集 ──
const diagnostics = {
  url,
  timestamp: new Date().toISOString(),
  consoleErrors: [],
  consoleWarns: [],
  networkErrors: [],
  wsEvents: [],
  screenshots: [],
};

console.log(`🎬 Visual Debug: ${url}`);
console.log(`   输出: ${gifPath}`);

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: mobile ? { width: 375, height: 812 } : { width: 1440, height: 900 },
  recordVideo: { dir: EVIDENCE_DIR, size: mobile ? { width: 375, height: 812 } : { width: 1440, height: 900 } },
});
const page = await context.newPage();

// ── 事件收集 ──
page.on('console', msg => {
  if (msg.type() === 'error') diagnostics.consoleErrors.push({ ts: Date.now(), text: msg.text() });
  if (msg.type() === 'warning') diagnostics.consoleWarns.push({ ts: Date.now(), text: msg.text() });
});

page.on('requestfailed', req => {
  diagnostics.networkErrors.push({
    ts: Date.now(),
    url: req.url(),
    failure: req.failure()?.errorText || 'unknown'
  });
});

page.on('websocket', ws => {
  const info = { url: ws.url(), ts: Date.now(), events: [] };
  ws.on('close', () => info.events.push({ ts: Date.now(), type: 'close' }));
  ws.on('framesent', f => info.events.push({ ts: Date.now(), type: 'send', data: f.payload?.substring(0, 200) }));
  ws.on('framereceived', f => info.events.push({ ts: Date.now(), type: 'recv', data: f.payload?.substring(0, 200) }));
  diagnostics.wsEvents.push(info);
});

// ── 登录（可选） ──
if (login) {
  const baseUrl = mobile ? 'http://localhost:3800' : 'http://localhost:3100';
  await page.goto(mobile ? `${baseUrl}/#/pages/login/index` : `${baseUrl}/#/login`);
  await page.waitForTimeout(3000);
  const inputs = await page.locator('input').all();
  if (inputs.length >= 2) {
    await inputs[0].fill('admin');
    await inputs[1].fill('123456');
  }
  if (mobile) {
    await page.locator('text=登 录').first().click().catch(() => {});
  } else {
    await page.locator('button:has-text("登录")').first().click().catch(() => {});
  }
  await page.waitForTimeout(4000);
  console.log('   ✅ 已登录');
}

// ── 导航到目标页面 ──
await page.goto(url);
await page.waitForTimeout(3000);
console.log(`   📍 已导航: ${page.url()}`);

// ── 录制期间持续截图 ──
const frameInterval = 500; // 每 500ms 一帧
const totalFrames = Math.floor((duration * 1000) / frameInterval);
console.log(`   🎞️  录制 ${duration}s (${totalFrames} 帧)...`);

for (let i = 0; i < totalFrames; i++) {
  await page.waitForTimeout(frameInterval);
}

// ── 最后一帧高清截图 ──
await page.screenshot({ path: pngPath, fullPage: false });
diagnostics.screenshots.push(pngPath);
console.log(`   📸 截图: ${pngPath}`);

// ── 关闭并保存视频 ──
await context.close();
await browser.close();

// ── 找 Playwright 生成的 webm 文件 ──
const files = readdirSync(EVIDENCE_DIR).filter(f => f.endsWith('.webm'));
const latestWebm = files.sort((a, b) => {
  return statSync(resolve(EVIDENCE_DIR, b)).mtimeMs - statSync(resolve(EVIDENCE_DIR, a)).mtimeMs;
})[0];

if (latestWebm) {
  const webmPath = resolve(EVIDENCE_DIR, latestWebm);
  // 尝试用 ffmpeg 转 gif
  try {
    execSync(`ffmpeg -i "${webmPath}" -vf "fps=10,scale=800:-1:flags=lanczos" -loop 0 "${gifPath}" -y 2>nul`, { stdio: 'ignore' });
    console.log(`   🎬 GIF: ${gifPath}`);
    // 删除 webm
    unlinkSync(webmPath);
  } catch {
    // 无 ffmpeg，保留 webm
    console.log(`   🎬 Video: ${webmPath} (安装 ffmpeg 可自动转 GIF)`);
  }
}

// ── 写诊断数据 ──
writeFileSync(dataPath, JSON.stringify(diagnostics, null, 2));
console.log(`   📊 诊断数据: ${dataPath}`);

// ── 摘要 ──
console.log(`\n📋 诊断摘要:`);
console.log(`   Console Errors: ${diagnostics.consoleErrors.length}`);
diagnostics.consoleErrors.slice(0, 5).forEach(e => console.log(`     ❌ ${e.text?.substring(0, 100)}`));
console.log(`   Network Errors: ${diagnostics.networkErrors.length}`);
diagnostics.networkErrors.slice(0, 5).forEach(e => console.log(`     🔌 ${e.url?.substring(0, 80)} — ${e.failure}`));
console.log(`   WebSocket Events: ${diagnostics.wsEvents.length} connections`);
diagnostics.wsEvents.forEach(w => console.log(`     🔗 ${w.url?.substring(0, 80)} (${w.events.length} events)`));

if (diagnostics.consoleErrors.length === 0 && diagnostics.networkErrors.length === 0) {
  console.log(`\n✅ 未检测到错误`);
} else {
  console.log(`\n⚠️  发现 ${diagnostics.consoleErrors.length + diagnostics.networkErrors.length} 个错误 — 请让 Agent 分析 ${dataPath}`);
}
