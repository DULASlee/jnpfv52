import { test, expect } from '@playwright/test';
import { loginAsAdmin } from '../helpers/login';

/** ~15s：只验登录，不碰 Studio */
test('admin 可登录', async ({ page }) => {
  await loginAsAdmin(page);
  expect(page.url()).not.toContain('/login');
});
