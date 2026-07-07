import { test, expect } from '@playwright/test';
import { loginAsAdmin, openSubmitRequirement } from '../helpers/login';
import { SubmitRequirementPage } from './pages/submit-requirement.page';

/**
 * ~2–4min：只验 SA 门控 UI（发送 → gate_passed 文案）。
 * 不等 PM Skill 完成；Analyst 不在范围内。
 *
 *   npx playwright test e2e/studio/04-submit-requirement-gate.spec.ts
 */
const REQUIREMENT =
  process.env.E2E_REQUIREMENT ||
  '请假系统：员工提交请假单，主管审批，HR归档；角色：员工/主管/HR。';

test.describe('提交需求页 · SA 门控', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openSubmitRequirement(page);
  });

  test('发送需求后出现门控通过提示', async ({ page }) => {
    test.setTimeout(240_000);

    const p = new SubmitRequirementPage(page);
    await p.sendRequirement(REQUIREMENT);

    await expect(p.gatePassedMessage()).toBeVisible({ timeout: 180_000 });
    await expect(p.gateFailedMessage()).not.toBeVisible();
  });
});
