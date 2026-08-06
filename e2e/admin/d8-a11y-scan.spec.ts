/**
 * D8 可访问性扫描 — axe-core via Playwright
 *
 * 覆盖关键路由: 登录页 + 首页 + 工作台 + 在线开发列表 + Studio 提交需求
 * 复用既有 loginAsAdmin helper (e2e/helpers/login.ts)
 *
 * 运行前置: dev server :3100 已启动 (start-dev.ps1)
 * 运行: cd e2e && npx playwright test admin/d8-a11y-scan.spec.ts --reporter=json
 * 证据: .claude/evidence/frontend-ct/d8-axe-results.json
 */
import { test } from '@playwright/test';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';
import AxeBuilder from '@axe-core/playwright';
import { loginAsAdmin } from '../helpers/login';

const OUT_DIR = join(__dirname, '..', '..', '.claude', 'evidence', 'frontend-ct');
mkdirSync(OUT_DIR, { recursive: true });

// 需登录的路由 — 登录后逐个访问并扫描
const AUTH_ROUTES = [
  { name: 'home', path: '/home' },
  { name: 'workStation', path: '/workStation' },
  { name: 'onlineDev-list', path: '/onlineDev' },
  { name: 'studio-submit', path: '/studio/ai/submit-requirement' },
];

test.describe('D8 可访问性扫描', () => {
  test('登录页 a11y', async ({ page }) => {
    await page.goto('/login', { waitUntil: 'domcontentloaded' });
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .analyze();
    writeResults('login', results);
  });

  test('登录后逐路由 a11y', async ({ page }) => {
    await loginAsAdmin(page);
    for (const r of AUTH_ROUTES) {
      await test.step(`扫描 ${r.name} (${r.path})`, async () => {
        await page.goto(r.path, { waitUntil: 'domcontentloaded', timeout: 30_000 });
        // 等待主内容渲染
        await page.waitForTimeout(2000);
        const results = await new AxeBuilder({ page })
          .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
          .exclude('.ant-message') // 动态通知,非内容
          .analyze();
        writeResults(r.name, results);
      });
    }
  });
});

interface AxeResult {
  name: string;
  violations: any[];
  passes: number;
  incomplete: number;
  inapplicable: number;
}

function writeResults(name: string, results: any) {
  const summary: AxeResult = {
    name,
    violations: results.violations.map((v: any) => ({
      id: v.id,
      impact: v.impact,
      description: v.description,
      help: v.help,
      tags: v.tags,
      nodeCount: v.nodes?.length || 0,
      sample: (v.nodes || []).slice(0, 3).map((n: any) => ({
        target: n.target,
        html: n.html?.slice(0, 200),
      })),
    })),
    passes: results.passes?.length || 0,
    incomplete: results.incomplete?.length || 0,
    inapplicable: results.inapplicable?.length || 0,
  };
  const file = join(OUT_DIR, `d8-axe-${name}.json`);
  writeFileSync(file, JSON.stringify(summary, null, 2));
  console.log(`[D8] ${name}: ${summary.violations.length} violations -> ${file}`);
}
