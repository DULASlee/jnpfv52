import { defineConfig } from '@playwright/test';

const baseURL = process.env.JNPF_WEB_URL || 'http://localhost:3100';

export default defineConfig({
  testDir: '.',
  testMatch: 'studio/**/*.spec.ts',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  retries: 0,
  reporter: [['list'], ['json', { outputFile: '../.claude/evidence/playwright-report.json' }]],
  use: {
    baseURL,
    headless: true,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { browserName: 'chromium' } }],
});
