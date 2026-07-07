import { test, expect } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loginAsAdmin, openSubmitRequirement } from '../helpers/login';
import { SubmitRequirementPage } from './pages/submit-requirement.page';

/**
 * 第 1 步 UI 验收 — 提交需求页 + 附件 + 门控 + 需求分析说明书（22 号 §6.1）
 *
 *   npx playwright test e2e/studio/05-step1-requirement-spec.spec.ts
 *
 * 前置：start-dev.ps1（:3100 + :5000 + :3001）
 * 耗时：约 10–20min（含 analyst-skill）
 */
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURE = path.resolve(__dirname, '../../scripts/fixtures/step1-leave-requirement.txt');

const REQUIREMENT =
  process.env.E2E_REQUIREMENT ||
  '请假系统：员工提交请假单，主管审批，HR归档；角色：员工/主管/HR；需报表与年假余额。';

test.describe('第1步 · 提交需求 → 需求分析说明书', () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openSubmitRequirement(page);
  });

  test('上传材料、对话、右侧产物栏出现 02-requirement-spec.md', async ({ page }) => {
    test.setTimeout(1_200_000);

    const p = new SubmitRequirementPage(page);
    await expect(p.observatoryPanel).toBeVisible({ timeout: 30_000 });

    await p.uploadAttachment(FIXTURE);
    await p.sendRequirement(REQUIREMENT);

    await expect(p.gatePassedMessage()).toBeVisible({ timeout: 240_000 });
    await expect(p.gateFailedMessage()).not.toBeVisible();

    await p.openDeliverablesTab();
    await expect(p.deliverableInPanel(/00-merged-requirement/)).toBeVisible({ timeout: 120_000 });
    await expect(p.deliverableInPanel(/01-skeleton/)).toBeVisible({ timeout: 600_000 });
    await expect(p.deliverableInPanel(/02-requirement-spec/)).toBeVisible({ timeout: 900_000 });
  });
});
