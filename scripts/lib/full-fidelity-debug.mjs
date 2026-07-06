#!/usr/bin/env node
/**
 * full-fidelity-debug — JNPF 全保真诊断采集器 (Trailblaze 模式)
 *
 * 每步捕获 5 层数据，支持事后不重跑诊断：
 *   1. HAR (HTTP Archive)   — 完整网络请求/响应
 *   2. DOM Snapshot         — 可访问性树 (accessibility tree)
 *   3. Console Log          — error/warning/info 分级
 *   4. Screenshot           — 全页 + 视口
 *   5. Tool Call Trace      — Action → Response → Next Action
 *
 * 用法:
 *   # 录制完整 session
 *   node scripts/lib/full-fidelity-debug.mjs --url=http://localhost:3100/#/studio/ai/submit-requirement --duration=30
 *
 *   # 带登录 + 自定义步骤
 *   node scripts/lib/full-fidelity-debug.mjs --login --steps=steps.json --output=gate-debug
 *
 *   # CI 模式（仅错误时输出）
 *   node scripts/lib/full-fidelity-debug.mjs --ci --url=... --duration=10
 *
 * 产出 (in .claude/evidence/):
 *   ff-<name>.json    — 完整诊断包 (供 AI Agent 分析)
 *   ff-<name>.har     — HAR 网络日志
 *   ff-<name>.png     — 最后一帧截图
 *
 * 与 visual-debug.mjs 的差异:
 *   - 不做 GIF/视频（太重），聚焦结构化数据
 *   - HAR 替代被动 network error 收集
 *   - DOM 快照替代纯截图
 *   - 支持自定义步骤脚本
 */

import { chromium } from 'playwright';
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const EVIDENCE_DIR = resolve(__dirname, '..', '..', '.claude', 'evidence');

// ── CLI 参数解析 ──
const args = process.argv.slice(2);
let url = '';
let duration = 30;
let login = false;
let output = '';
let ciMode = false;
let stepsFile = '';

for (let i = 0; i < args.length; i++) {
  if (args[i] === '--url' || args[i] === '-u') url = args[++i];
  else if (args[i] === '--duration' || args[i] === '-d') duration = parseInt(args[++i]) || 30;
  else if (args[i] === '--login' || args[i] === '-l') login = true;
  else if (args[i] === '--output' || args[i] === '-o') output = args[++i];
  else if (args[i] === '--ci') ciMode = true;
  else if (args[i] === '--steps' || args[i] === '-s') stepsFile = args[++i];
}

if (!url) {
  console.log('用法: node scripts/lib/full-fidelity-debug.mjs --url=<URL> [--login] [--duration=30] [--output=name] [--ci] [--steps=steps.json]');
  process.exit(1);
}

if (!existsSync(EVIDENCE_DIR)) mkdirSync(EVIDENCE_DIR, { recursive: true });

const timestamp = output || `ff-${Date.now()}`;
const jsonPath = resolve(EVIDENCE_DIR, `${timestamp}.json`);
const harPath = resolve(EVIDENCE_DIR, `${timestamp}.har`);
const pngPath = resolve(EVIDENCE_DIR, `${timestamp}.png`);

// ── 全保真诊断包 ──
const report = {
  meta: {
    url,
    timestamp: new Date().toISOString(),
    duration,
    login,
    ciMode,
    stepsFile: stepsFile || null,
  },
  steps: [],
  console: { errors: [], warnings: [], infos: [] },
  network: { har: null, errors: [] },
  domSnapshots: [],
  wsEvents: [],
};

console.log(`🔬 Full-Fidelity Debug: ${url}`);
console.log(`   输出: ${jsonPath}`);

// ── 启动浏览器 ──
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  recordHar: { path: harPath, mode: 'full' },
});
const page = await context.newPage();

// ── 事件收集 ──
page.on('console', msg => {
  const entry = { ts: Date.now(), text: msg.text(), loc: msg.location() };
  if (msg.type() === 'error') report.console.errors.push(entry);
  else if (msg.type() === 'warning') report.console.warnings.push(entry);
  else report.console.infos.push(entry);
});

page.on('requestfailed', req => {
  report.network.errors.push({
    ts: Date.now(),
    url: req.url(),
    method: req.method(),
    failure: req.failure()?.errorText || 'unknown',
  });
});

page.on('websocket', ws => {
  const info = { url: ws.url(), ts: Date.now(), events: [] };
  ws.on('close', () => info.events.push({ ts: Date.now(), type: 'close' }));
  ws.on('framesent', f => info.events.push({ ts: Date.now(), type: 'send', data: f.payload?.substring(0, 500) }));
  ws.on('framereceived', f => info.events.push({ ts: Date.now(), type: 'recv', data: f.payload?.substring(0, 500) }));
  report.wsEvents.push(info);
});

// ── DOM 快照采集 ──
async function captureDomSnapshot(label) {
  try {
    const snapshot = await page.accessibility.snapshot({ interestingOnly: false });
    report.domSnapshots.push({ label, ts: Date.now(), tree: snapshot });
  } catch {
    // 某些页面不支持 accessibility tree
  }
}

// ── 记录步骤 ──
let stepIndex = 0;
async function recordStep(action, detail = {}) {
  stepIndex++;
  const step = {
    index: stepIndex,
    ts: Date.now(),
    action,
    detail,
    url: page.url(),
    title: await page.title().catch(() => ''),
  };
  report.steps.push(step);
  if (!ciMode) console.log(`   📍 Step ${stepIndex}: ${action}`);
}

// ── 登录 ──
if (login) {
  await recordStep('navigate:login');
  await page.goto('http://localhost:3100/#/login', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);
  await captureDomSnapshot('login-page');

  const accountInput = page.getByTestId('login-account-input').or(page.locator('input[placeholder*="账号"]')).first();
  const passwordInput = page.getByTestId('login-password-input').or(page.locator('input[type="password"]')).first();
  const submitBtn = page.getByTestId('login-submit-btn').or(page.getByRole('button', { name: /登录/i })).first();

  await accountInput.fill('admin');
  await passwordInput.fill('123456');
  await recordStep('click:login');
  await submitBtn.click();

  try {
    await page.waitForURL(/\/(home|workStation|dashboard|studio)/, { timeout: 30_000 });
    await recordStep('navigate:home');
  } catch {
    report.console.errors.push({ ts: Date.now(), text: '登录超时: 30s 内未跳转', loc: {} });
  }
  await page.waitForTimeout(2000);
  await captureDomSnapshot('after-login');
}

// ── 导航到目标页面 ──
await recordStep('navigate:target', { url });
await page.goto(url, { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(3000);
await captureDomSnapshot('target-page');

// ── 自定义步骤（如提供） ──
const customSteps = [];
if (stepsFile) {
  try {
    const { readFileSync } = await import('fs');
    const raw = readFileSync(stepsFile, 'utf8');
    customSteps.push(...JSON.parse(raw));
    console.log(`   📋 加载 ${customSteps.length} 个自定义步骤`);
  } catch (e) {
    console.error(`   ⚠️ 步骤文件解析失败: ${e.message}`);
  }
}

// ── 执行步骤（自定义或被动录制） ──
if (customSteps.length > 0) {
  for (const step of customSteps) {
    await recordStep(step.action, step);
    switch (step.action) {
      case 'click':
        await page.locator(step.selector).click().catch(() => {});
        break;
      case 'fill':
        await page.locator(step.selector).fill(step.value || '').catch(() => {});
        break;
      case 'wait':
        await page.waitForTimeout(step.ms || 2000);
        break;
      case 'snapshot':
        await captureDomSnapshot(step.label || `step-${stepIndex}`);
        break;
      case 'screenshot':
        // 中间截图在最后统一处理
        break;
      default:
        console.log(`   ⚠️ 未知步骤类型: ${step.action}`);
    }
    await page.waitForTimeout(500);
  }
} else {
  // 被动模式：仅等待指定时长，期间持续记录 DOM 快照
  const snapInterval = 5000; // 每 5s 一张 DOM 快照
  const totalSnaps = Math.max(1, Math.floor((duration * 1000) / snapInterval));
  console.log(`   ⏱️  被动录制 ${duration}s (${totalSnaps} 次 DOM 快照)...`);

  for (let i = 0; i < totalSnaps; i++) {
    await page.waitForTimeout(snapInterval);
    await captureDomSnapshot(`passive-${i + 1}`);
    await recordStep('passive-snapshot', { elapsed: (i + 1) * snapInterval });
  }
}

// ── 最终状态采集 ──
await page.screenshot({ path: pngPath, fullPage: true });
console.log(`   📸 截图: ${pngPath}`);

await captureDomSnapshot('final');

// ── 关闭浏览器，保存 HAR ──
await context.close();
await browser.close();

// ── 组装报告 ──
const harData = existsSync(harPath)
  ? { path: harPath, size: (await import('fs')).statSync(harPath).size }
  : null;
report.network.har = harData;

report.summary = {
  stepCount: report.steps.length,
  domSnapshotCount: report.domSnapshots.length,
  consoleErrorCount: report.console.errors.length,
  consoleWarnCount: report.console.warnings.length,
  networkErrorCount: report.network.errors.length,
  wsConnectionCount: report.wsEvents.length,
  health: report.console.errors.length === 0 && report.network.errors.length === 0 ? 'clean' : 'issues-found',
};

writeFileSync(jsonPath, JSON.stringify(report, null, 2));
console.log(`\n📊 全保真报告: ${jsonPath}`);
console.log(`   HAR: ${harPath}`);
console.log(`   步骤: ${report.summary.stepCount} | DOM快照: ${report.summary.domSnapshotCount}`);
console.log(`   错误: Console ${report.summary.consoleErrorCount} | Network ${report.summary.networkErrorCount}`);
console.log(`   健康度: ${report.summary.health === 'clean' ? '✅ 无异常' : '⚠️ 发现问题'}`);

if (report.summary.health !== 'clean') {
  console.log(`\n⚠️  错误摘要:`);
  report.console.errors.slice(0, 5).forEach(e => console.log(`   ❌ ${e.text?.substring(0, 120)}`));
  report.network.errors.slice(0, 5).forEach(e => console.log(`   🔌 ${e.url?.substring(0, 80)} — ${e.failure}`));
  process.exit(1);
} else {
  console.log(`\n✅ 全保真采集完成，未检测到错误`);
}
