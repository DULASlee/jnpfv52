import type { Page } from '@playwright/test';

const ACCOUNT = process.env.JNPF_ACCOUNT || 'admin';
const PASSWORD = process.env.JNPF_PASSWORD || '123456';

/**
 * 管理员登录（自愈选择器：data-testid > getByRole > fallback）
 *
 * 2026-07 自愈改造：优先使用 data-testid（不受 UI 文案/样式变更影响），
 * 保留 getByRole fallback 以兼容未部署新前端的旧环境。
 */
export async function loginAsAdmin(page: Page) {
  await page.goto('/#/login', { waitUntil: 'domcontentloaded' });

  // ① data-testid 优先
  const accountInput = page.getByTestId('login-account-input');
  const passwordInput = page.getByTestId('login-password-input');
  const submitBtn = page.getByTestId('login-submit-btn');

  // ② fallback: 旧前端未部署 data-testid 时的语义选择器
  const accountFallback = page.locator('input[placeholder*="账号"], input[placeholder*="账户"]');
  const passwordFallback = page.locator('input[type="password"]');
  const submitFallback = page.getByRole('button', { name: /登录|登錄|Login/i });

  // 哪个先可见就用哪个
  const acct = accountInput.or(accountFallback).first();
  const pwd = passwordInput.or(passwordFallback).first();
  const submit = submitBtn.or(submitFallback).first();

  await acct.fill(ACCOUNT);
  await pwd.fill(PASSWORD);
  await submit.click();
  await page.waitForURL(/\/(home|workStation|dashboard|studio)/, { timeout: 30_000 }).catch(() => {});
}

/**
 * 打开提交需求页面
 */
export async function openSubmitRequirement(page: Page, pipelineId?: number) {
  const q = pipelineId ? `?pipelineId=${pipelineId}` : '';
  await page.goto(`/#/studio/ai/submit-requirement${q}`, { waitUntil: 'domcontentloaded' });

  // ① data-testid 优先
  const textarea = page.getByTestId('submit-requirement-textarea');

  // ② fallback: 旧前端
  const textareaFallback = page.locator('.input-bar textarea');

  await textarea.or(textareaFallback).first().waitFor({ state: 'visible', timeout: 20_000 });
}
