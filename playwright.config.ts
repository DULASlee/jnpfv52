import { defineConfig, devices } from '@playwright/test';

/**
 * JNPF v5.2 E2E 配置
 *
 * 用途：Supreme Iron Law 的 E1 证据产出（浏览器端到端截图）。
 * 详细操作流程见 .claude/skills/playwright/SKILL.md
 *
 * 运行：
 *   npx playwright test                    # 跑全部 spec
 *   npx playwright test --headed           # 有头模式（调试）
 *   npx playwright test e2e/smoke.spec.ts  # 单个 spec
 *
 * 截图默认输出到 .claude/evidence/（被 guard-finish.mjs 检查）
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: [['list']],

  // 截图落盘到 Supreme Iron Law 指定的证据目录
  outputDir: '.claude/evidence/playwright-trace',

  use: {
    // JNPF 前端 dev server（见 start-dev.ps1）
    baseURL: process.env.JNPF_BASE_URL || 'http://localhost:3100',

    // 失败截图 → 自动成为 E1 证据
    screenshot: 'only-on-failure',

    trace: 'retain-on-failure',
    viewport: { width: 1440, height: 900 },
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // 不自动启动 dev server —— 由 start-dev.ps1 统一管理（CLAUDE.md 铁律）
  // 如需自动启停，取消注释并填入 webServer 配置
});
