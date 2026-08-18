import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../helpers/login';

/**
 * 验证 admin 头像下拉菜单含"切换系统"入口，点击后打开 SystemTriggerDrawer 抽屉。
 * Supreme Iron Law E1/E2/E3 证据。
 */
test('admin 头像下拉含"切换系统"且打开抽屉', async ({ page }) => {
  await loginAsAdmin(page);

  // 点 admin 头像展开下拉（class 含 header-user-dropdown，兼容 namespace 前缀）
  const trigger = page.locator('[class*="header-user-dropdown"]').first();
  await trigger.waitFor({ state: 'visible', timeout: 15_000 });
  await trigger.click();

  // 找到"切换系统"菜单项（data-testid 优先，文案 fallback）
  const switchItem = page
    .getByTestId('user-dropdown-switch-system')
    .or(page.getByRole('menuitem', { name: /切换系统|系统切换/ }))
    .first();
  await expect(switchItem).toBeVisible({ timeout: 10_000 });

  // E1 截图：下拉菜单展开（含切换系统项）
  await page.screenshot({
    path: '.claude/evidence/admin-system-switch-dropdown.png',
    fullPage: false,
  });

  // 点击切换系统
  await switchItem.click();

  // 验证抽屉打开（SystemTriggerDrawer title="切换应用"）
  const drawer = page
    .locator('.portal-toggle-drawer')
    .or(page.getByText('切换应用'))
    .first();
  await expect(drawer).toBeVisible({ timeout: 10_000 });

  // E1 截图：抽屉展开（显示功能演示/开发平台）
  await page.screenshot({
    path: '.claude/evidence/admin-system-switch-drawer.png',
    fullPage: false,
  });
});
