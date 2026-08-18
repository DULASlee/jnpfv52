import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loginAsAdmin, openSubmitRequirement } from '../helpers/login';
import { SubmitRequirementPage } from './pages/submit-requirement.page';
import { login as apiLogin, apiRequest, jnpfData, isJnpfOk } from '../../scripts/lib/jnpf-auth.mjs';

/**
 * Studio 业务验收唯一主入口（替代 mjs 长链假绿）
 *
 * 原则：模拟真人操作，断言用户可感知的业务结果，不断言「文件名碰巧存在」。
 *
 *   pnpm e2e:studio:business
 *
 * 前置：start-dev.ps1（:3100 + :5000）
 */
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURE = path.resolve(__dirname, '../../scripts/fixtures/business-locker-requirement.txt');
const MIN_EXTRACTED = Number(process.env.E2E_MIN_EXTRACTED || 10_000);

const REQUIREMENT =
  process.env.E2E_REQUIREMENT ||
  '请为我开发这个系统：智能更衣柜管理系统，需支持柜位分配、人脸/指纹开柜、权限回收与审计。';

test.describe('Studio 业务验收（唯一主入口）', () => {
  test.beforeAll(() => {
    expect(fs.existsSync(FIXTURE), `缺少大附件 fixture: ${FIXTURE}`).toBe(true);
    expect(fs.statSync(FIXTURE).size, 'fixture 必须足够大以覆盖截断类缺陷').toBeGreaterThanOrEqual(15_000);
  });

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await openSubmitRequirement(page);
  });

  test('上传大附件 → 解析 → 门控 → PM：业务结果全绿', async ({ page }) => {
    test.setTimeout(1_200_000);

    const p = new SubmitRequirementPage(page);

    // 左侧任务栏可见即可（观测台可关闭，不作为前置）
    await expect(p.stageSidebar).toBeVisible({ timeout: 30_000 });
    await expect(p.textarea).toBeVisible();

    // ① 真实浏览器上传（覆盖 axios FormData 路径）
    await p.uploadAttachment(FIXTURE);
    await expect(p.attList.getByText(/business-locker-requirement/)).toBeVisible({ timeout: 15_000 });

    await p.sendRequirement(REQUIREMENT);

    // ② 页面不得出现对象字符串化乱码
    await expect(p.objectObjectGarbage()).toHaveCount(0);

    // ③ 附件解析结果出现在对话流（attachments_ready）
    await expect(p.attachmentParsedInChat()).toBeVisible({ timeout: 180_000 });

    const pipelineId = await p.waitPipelineIdInUrl(180_000);
    expect(pipelineId, 'URL 应出现 pipelineId').toBeGreaterThan(0);

    // ④ API 业务断言：提取字数（覆盖 NVARCHAR 截断 / 假解析）
    const session = await apiLogin();
    await expect
      .poll(
        async () => {
          const res = await apiRequest('GET', `/api/studio/pipeline/execute/${pipelineId}/attachments`, { session });
          if (!isJnpfOk(res) && res.status !== 200) return 0;
          const data = jnpfData(res) ?? res.json?.data ?? res.json;
          const items = data?.items ?? data?.Items ?? [];
          const maxLen = Math.max(
            0,
            ...items.map((it: any) => Number(it.extractedLength ?? it.ExtractedLength ?? 0)),
          );
          const okStatus = items.some((it: any) => Number(it.processStatus ?? it.ProcessStatus) === 2);
          return okStatus ? maxLen : 0;
        },
        { timeout: 180_000, intervals: [2_000, 3_000, 5_000] },
      )
      .toBeGreaterThanOrEqual(MIN_EXTRACTED);

    // ⑤ 门控：人类可读通过文案（失败也必须不是 [object Object]）
    await expect(p.gatePassedMessage()).toBeVisible({ timeout: 300_000 });
    await expect(p.objectObjectGarbage()).toHaveCount(0);

    // ⑥ PM：不得出现 ToT 全灭（本周真实故障）
    // 等待一段时间让 PM 推进；若出现致命文案立即失败
    const deadline = Date.now() + 600_000;
    let pmFailed = false;
    while (Date.now() < deadline) {
      if (await p.pmTotAllBranchesFailed().count()) {
        pmFailed = true;
        break;
      }
      // 骨架确认/业务事件出现 → PM 业务成功
      if (await p.skeletonConfirmCard().isVisible().catch(() => false)) break;
      if (await p.chatStream.getByText(/01-skeleton|IR-0|业务事件/).first().isVisible().catch(() => false)) break;
      await page.waitForTimeout(5_000);
    }
    expect(pmFailed, 'PM Skill ToT 全部分支产出无效 — 业务失败').toBe(false);
    await expect(p.objectObjectGarbage()).toHaveCount(0);

    // 终态：至少看到推理块 + 门控通过（骨架/澄清任一即可）
    await expect(p.thinkingWorkflowBlock()).toBeVisible();
    await expect(p.gatePassedMessage()).toBeVisible();
  });
});
