#!/usr/bin/env node
/**
 * 诊断：提交需求后 PM 步骤③是否卡住（SSE + thinking 区采样）
 * 用法: node scripts/probe-pm-step3-stuck.mjs
 */
import { chromium } from 'playwright';
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const EVIDENCE = resolve(__dirname, '..', '.claude', 'evidence');
if (!existsSync(EVIDENCE)) mkdirSync(EVIDENCE, { recursive: true });

const FE = 'http://localhost:3100';
const REQ =
  '我要做一个请假管理系统：员工提交请假单，主管审批，HR备案；需要年假/事假/病假类型，审批通过后扣减年假余额。';

const samples = [];
const sseEvents = [];

async function loginUi(page) {
  await page.goto(`${FE}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[placeholder*="账号"], input[placeholder*="账户"]').first().fill('admin');
  await page.fill('input[type="password"]', '123456');
  await page.getByRole('button', { name: /登.*录/ }).click();
  await page.waitForURL(/\/(home|workStation|dashboard|studio)/, { timeout: 30_000 });
}

async function waitForTextarea(page, timeoutMs = 30_000) {
  const sel = page.getByTestId('submit-requirement-textarea').or(page.locator('.input-bar textarea')).first();
  await sel.waitFor({ state: 'visible', timeout: timeoutMs });
  return sel;
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  page.on('response', async (res) => {
    const url = res.url();
    if (!url.includes('/api/studio/pipeline/execute/') || !url.includes('/events')) return;
    try {
      const text = await res.text();
      for (const line of text.split('\n')) {
        if (!line.startsWith('data: ') || line === 'data: [DONE]') continue;
        try {
          const data = JSON.parse(line.slice(6));
          sseEvents.push({ ts: Date.now(), type: data.type || data.event, preview: JSON.stringify(data).slice(0, 300) });
        } catch { /* skip */ }
      }
    } catch { /* stream */ }
  });

  console.log('1. UI 登录…');
  await loginUi(page);

  console.log('2. 打开提交需求页…');
  await page.goto(`${FE}/studio/ai/submit-requirement`, { waitUntil: 'domcontentloaded' });
  const textarea = await waitForTextarea(page);
  await page.screenshot({ path: resolve(EVIDENCE, 'probe-pm-step3-before-send.png'), fullPage: true });

  console.log('3. 发送需求…');
  await textarea.fill(REQ);
  const sendBtn = page.getByTestId('submit-requirement-send-btn').or(page.locator('.input-bar button').last());
  await sendBtn.first().click();

  const start = Date.now();
  const maxMs = 300_000;
  let lastThinking = '';
  let lastSseCount = 0;

  while (Date.now() - start < maxMs) {
    await page.waitForTimeout(5000);
    const thinking = await page.locator('.thinking-content').last().textContent().catch(() => '');
    const chatText = await page.locator('[data-testid="chat-stream"]').textContent().catch(() => '');
    const hasClarification = await page.locator('.clarification-card, [class*="clarification"]').count().catch(() => 0);
    const hasSpecConfirm = await page.getByText('需求说明书确认').count().catch(() => 0);

    const elapsed = Math.round((Date.now() - start) / 1000);
    const sseNew = sseEvents.length - lastSseCount;
    lastSseCount = sseEvents.length;

    const sample = {
      elapsedSec: elapsed,
      thinkingLen: (thinking || '').length,
      thinkingTail: (thinking || '').slice(-400),
      chatHasDeepen: (chatText || '').includes('深度优化'),
      chatHasNineStep: (chatText || '').includes('九步'),
      clarificationCards: hasClarification,
      specConfirm: hasSpecConfirm > 0,
      sseNewEvents: sseNew,
      lastSseType: sseEvents.at(-1)?.type,
    };
    const pipelineId = await page.evaluate(() => {
      const m = location.href.match(/pipelineId=(\d+)/);
      return m ? Number(m[1]) : 0;
    });
    if (pipelineId) sample.pipelineId = pipelineId;
    samples.push(sample);

    console.log(
      `[${elapsed}s] pid=${pipelineId || '?'} thinking=${sample.thinkingLen} chars, sse+${sseNew}, clar=${hasClarification}, deepen=${sample.chatHasDeepen || (thinking||'').includes('深度优化')}, tail=${sample.thinkingTail.slice(-80).replace(/\s+/g, ' ')}`,
    );

    if (hasClarification > 0 || hasSpecConfirm > 0) {
      console.log('✅ 已出现追问卡片或说明书确认，未卡死');
      break;
    }

    const hasDeepenMsg = (thinking || '').includes('深度优化') || (thinking || '').includes('结构化追问');
    if (hasDeepenMsg && elapsed > 120 && hasClarification === 0) {
      console.log('⚠️ 深度优化提示已出但 120s 内无追问卡片');
    }

    if (thinking === lastThinking && elapsed > 180 && sseNew === 0) {
      console.log('⚠️ 180s+ 无 thinking/SSE 变化，判定卡住');
      break;
    }
    lastThinking = thinking || '';
  }

  const png = resolve(EVIDENCE, `probe-pm-step3-${Date.now()}.png`);
  await page.screenshot({ path: png, fullPage: true });

  const report = {
    req: REQ,
    durationSec: Math.round((Date.now() - start) / 1000),
    samples,
    sseEvents: sseEvents.slice(-50),
    screenshot: png,
  };
  const jsonPath = resolve(EVIDENCE, `probe-pm-step3-${Date.now()}.json`);
  writeFileSync(jsonPath, JSON.stringify(report, null, 2));
  console.log(`\n报告: ${jsonPath}\n截图: ${png}`);

  await browser.close();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
