import { test, expect } from '@playwright/test';
import { loginAsAdmin, openSubmitRequirement } from '../helpers/login';
import { SubmitRequirementPage } from './pages/submit-requirement.page';

/** ~30s：只验页面控件存在，不发送、不等 Skill */
test.describe('提交需求页 · 布局', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openSubmitRequirement(page);
  });

  test('三栏 + 输入框 + 发送按钮', async ({ page }) => {
    const p = new SubmitRequirementPage(page);
    await expect(p.stageSidebar).toBeVisible();
    await expect(p.textarea).toBeVisible();
    await expect(p.sendBtn).toBeVisible();
    await expect(p.chatStream).toBeVisible();
  });
});
