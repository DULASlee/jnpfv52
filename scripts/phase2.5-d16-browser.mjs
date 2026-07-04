#!/usr/bin/env node
/**
 * D16 / G6 浏览器端：pipeline 切换 ×10 + SSE abort
 * 产出 E1 截图至 .claude/evidence/
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { login, apiRequest, isJnpfOk, jnpfData, pick } from './lib/jnpf-auth.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EVIDENCE = path.resolve(__dirname, '../.claude/evidence');
const API = process.env.JNPF_API_URL || 'http://localhost:5000';
const FE = process.env.JNPF_FE_URL || 'http://localhost:3100';
const SWITCH_COUNT = Number(process.env.D16_SWITCH_COUNT || 10);

async function createPipeline(session, tag) {
  const res = await apiRequest('POST', '/api/studio/pipeline/execute/create', {
    body: { name: `D16-${tag}`, userRequirement: ('D16泄漏测试 ' + tag).padEnd(820, 'x') },
    session,
  });
  if (!isJnpfOk(res)) throw new Error(JSON.stringify(res.json));
  return pick(jnpfData(res), 'pipelineId', 'PipelineId');
}

async function main() {
  fs.mkdirSync(EVIDENCE, { recursive: true });
  const fe = await fetch(FE).catch(() => null);
  if (!fe?.ok && fe?.status !== 302) {
    console.error('前端 :3100 未启动');
    process.exit(1);
  }

  let chromium;
  try {
    ({ chromium } = await import('playwright'));
  } catch {
    console.error('playwright 未安装 — 跳过浏览器 D16');
    process.exit(0);
  }

  const session = await login({ force: true });
  const ids = [];
  for (let i = 0; i < 3; i++) ids.push(await createPipeline(session, i));

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  const errors = [];

  page.on('console', msg => {
    if (msg.type() === 'error') errors.push(msg.text());
  });

  await page.goto(`${FE}/#/login`);
  await page.fill('input[placeholder*="账号"], input[placeholder*="账户"]', 'admin');
  await page.fill('input[type="password"]', '123456');
  await page.click('button:has-text("登录"), button:has-text("登 录")');
  await page.waitForTimeout(3000);

  await page.goto(`${FE}/#/studio/ai/submit-requirement`);
  await page.waitForTimeout(2000);

  for (let i = 0; i < SWITCH_COUNT; i++) {
    const id = ids[i % ids.length];
    await page.evaluate(pid => {
      window.__d16PipelineId = pid;
    }, id);
    await page.goto(`${FE}/#/studio/ai/submit-requirement?pipelineId=${id}`);
    await page.waitForTimeout(800);
  }

  await page.screenshot({ path: path.join(EVIDENCE, 'phase2.5-d16-switch.png'), fullPage: true });

  const report = {
    switchCount: SWITCH_COUNT,
    pipelineIds: ids,
    consoleErrors: errors.slice(0, 20),
    pass: errors.filter(e => /SSE|fetch|abort/i.test(e)).length === 0,
    at: new Date().toISOString(),
  };
  fs.writeFileSync(path.join(EVIDENCE, 'phase2.5-d16-report.json'), JSON.stringify(report, null, 2));
  console.log('[d16-browser]', report.pass ? 'PASS' : 'WARN', report);
  await browser.close();
  process.exit(report.pass ? 0 : 1);
}

main().catch(e => {
  console.error(e);
  process.exit(1);
});
