import { test, expect } from '@playwright/test';
import { loginAsAdmin, openSubmitRequirement } from '../helpers/login';
import { SubmitRequirementPage } from './pages/submit-requirement.page';

/**
 * ~1min：只验已有 pipeline 的 S0 交付物 UI。
 * 必须提供已跑过门控的 pipeline，不创建、不等 PM/Analyst。
 *
 *   E2E_PIPELINE_ID=294 npx playwright test e2e/studio/03-submit-requirement-deliverables.spec.ts
 */
const pipelineId = Number(process.env.E2E_PIPELINE_ID || 0);

test.describe('提交需求页 · S0 交付物', () => {
  test.skip(!pipelineId, '设置 E2E_PIPELINE_ID=已有pipelineId');

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openSubmitRequirement(page, pipelineId);
  });

  test('交付物栏显示 gate 报告下载', async ({ page }) => {
    const p = new SubmitRequirementPage(page);
    await expect(p.deliverableLinks).toBeVisible({ timeout: 15_000 });
    await expect(p.deliverableButton(/00-gate-report/)).toBeVisible();
  });
});
