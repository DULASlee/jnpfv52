#!/usr/bin/env node
/**
 * 架构设计 Skill 端到端验证
 *
 * 链路：创建pipeline → SA门控 → PM骨架 → 确认骨架 → Analyst九步 → 架构设计skill
 * 产出：.claude/evidence/arch-skill-verify.json + 控制台 dump 关键产出
 *
 * 用法：node scripts/arch-skill-verify.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  assertDeliverableNames,
  confirmSkeleton,
  createPipeline,
  getDeliverables,
  getEvents,
  getSnapshots,
  log,
  probeSaService,
  runAnalystSkill,
  runPmSkill,
  triggerSaGate,
  waitDeliverable,
  waitSkillTerminal,
  writeEvidence,
} from './lib/phase-sup-api.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const steps = [];

function step(name, pass, detail, extra = {}) {
  steps.push({ name, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', name, detail);
  return pass;
}

// 高质量业务需求 — MES 工厂报工场景（确保门控易通过：含业务事件/角色/实体/字段）
const REQUIREMENT_TEXT = `【项目背景】
某机械制造工厂需要一套车间生产报工管理系统，替代当前纸质工单流程。

【核心业务场景】
1. 工单下达：生产计划员根据月度生产计划创建工单，明确产品型号、计划数量、交货日期、所需工序。
2. 工序报工：车间工人完成一道工序后，扫描工单条码，提交报工记录，包括：完成数量、合格数量、废品数量、耗时、操作设备。
3. 质量检验：质检员对报工记录进行抽检，记录质量检验结果（合格/返工/报废），出具质检单。
4. 异常处理：发现设备故障或工艺异常时，工人提交异常报告，班组长审核并派工处理。
5. 工资核算：月末统计员根据合格报工数量 × 计件单价，计算工人计件工资。

【参与角色】
- 生产计划员：下达工单
- 车间工人：工序报工、提交异常
- 质检员：质量检验
- 班组长：异常审核派工
- 统计员：工资核算
- 车间主任：查看报表、审批

【核心数据】
工单（工单号、产品型号、计划数量、工序清单、交货日期、状态）
报工记录（报工单号、工单号、工序、工人、完成数量、合格数量、废品数量、设备、报工时间）
质检单（质检单号、报工记录、检验结果、检验员、检验时间）
异常报告（异常单号、工单号、类型、描述、提交人、状态、处理结果）
计件工资（月份、工人、工单、合格数量、单价、金额）

【期望】
支持条码扫描报工、移动端操作、实时生产看板、月度工资报表导出。`;

async function main() {
  log('=== 架构设计 Skill 端到端验证 ===', '');

  const { login } = await import('./lib/jnpf-auth.mjs');
  const session = await login();
  log('logged in as', session.account);

  // 0. 探测 sa-service
  const saUp = await probeSaService();
  step('sa-service-probe', saUp, saUp ? 'reachable' : 'DOWN');
  if (!saUp) throw new Error('sa-service :3001 不可达，无法跑 analyst');

  // 1. 创建 pipeline
  const pipelineId = await createPipeline(session, `ARCH-VERIFY-${Date.now()}`, REQUIREMENT_TEXT);
  step('create-pipeline', true, `pipelineId=${pipelineId}`, { pipelineId });

  // 2. SA 门控
  await triggerSaGate(session, pipelineId, { autoRunPm: false, userText: REQUIREMENT_TEXT });
  step('sa-gate-trigger', true, 'async gate started');

  try {
    await waitDeliverable(session, pipelineId, '00-merged-requirement.md', 180_000);
    step('gate-passed', true, '00-merged-requirement.md 落盘');
  } catch (e) {
    step('gate-passed', false, e.message);
    throw e;
  }

  // 3. PM Skill → 骨架
  await runPmSkill(session, pipelineId);
  const pmResult = await waitSkillTerminal(session, pipelineId, 'pm-skill', 300_000);
  step('pm-skill', pmResult.status === 'completed', pmResult.status, { error: pmResult.error });
  if (pmResult.status !== 'completed') throw new Error('PM skill 未完成');

  await waitDeliverable(session, pipelineId, '01-skeleton.md', 60_000);
  step('skeleton-deliverable', true, '01-skeleton.md');

  // dump 骨架内容（业务事件分解情况）
  const skeletonDump = await dumpDeliverable(session, pipelineId, '01-skeleton.md');
  console.log('\n═══════════ PM 骨架产出（业务事件分解） ═══════════');
  console.log(String(skeletonDump).slice(0, 2000));
  console.log('═══════════════════════════════════════════════\n');

  // 4. 确认骨架 → Stable
  await confirmSkeleton(session, pipelineId, false);
  step('confirm-skeleton', true, 'IR-0 Stable');

  // 5. Analyst 九步 → 02-requirement-spec.md
  await runAnalystSkill(session, pipelineId);
  const analystResult = await waitSkillTerminal(session, pipelineId, 'analyst-skill', 600_000);
  step('analyst-skill', analystResult.status === 'completed', analystResult.status, { error: analystResult.error });
  if (analystResult.status !== 'completed') throw new Error('Analyst skill 未完成');

  await waitDeliverable(session, pipelineId, '02-requirement-spec.md', 120_000);
  step('requirement-spec', true, '02-requirement-spec.md');

  // 6. 确认需求阶段 → 触发架构（StageConfirmSkillTrigger.Requirement → architect）
  const confirmRes = await fetch(`http://localhost:5000/api/studio/pipeline/execute/stage/${pipelineId}/confirm`, {
    method: 'POST',
    headers: {
      'Authorization': 'Bearer ' + session.token,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ approved: true, comment: '架构设计验证' }),
  }).catch(e => { throw new Error('stage confirm failed: ' + e.message); });
  step('stage-confirm-requirement', confirmRes.ok, `HTTP ${confirmRes.status}`);

  // 7. 等待架构 skill 完成（由 StageConfirmSkillTrigger 调度）
  log('等待', 'architect-skill', '（由阶段确认触发）');
  const archResult = await waitSkillTerminal(session, pipelineId, 'architect-skill', 300_000);
  step('architect-skill', archResult.status === 'completed', archResult.status, { error: archResult.error });

  // 8. 检查架构产出物
  await waitDeliverable(session, pipelineId, '03-architecture.md', 60_000);
  step('architecture-deliverable', true, '03-architecture.md');

  // 9. dump 架构产出
  const archDump = await dumpDeliverable(session, pipelineId, '03-architecture.md');
  console.log('\n═══════════ 架构设计产出（03-architecture.md） ═══════════');
  console.log(archDump);
  console.log('═══════════════════════════════════════════════════════\n');

  // 10. 检查 IR 事件
  const events = (await getEvents(session, pipelineId)).map(e => e.eventType || e.EventType);
  const hasArchEvent = events.includes('ArchitectureDecisionRecorded');
  step('architecture-ir-event', hasArchEvent, `ArchitectureDecisionRecorded ${hasArchEvent ? '存在' : '缺失'}`, { eventTypes: events });

  // 11. 检查 IR 快照
  const snapshots = await getSnapshots(session, pipelineId);
  const archFragment = (snapshots || []).find(s => (s.fragmentType || s.FragmentType) === 'IR2_Architecture');
  step('architecture-ir-fragment', !!archFragment, archFragment ? `FragmentId=${archFragment.fragmentId || archFragment.FragmentId}` : '缺失');

  // 写证据
  const report = { pipelineId, steps, pass: steps.every(s => s.pass) };
  writeEvidence('arch-skill-verify.json', report);
  log('evidence →', 'arch-skill-verify.json', '');
  process.exit(report.pass ? 0 : 1);
}

async function dumpDeliverable(session, pipelineId, relativePath) {
  try {
    const res = await fetch(`http://localhost:5000/api/studio/pipeline/execute/${pipelineId}/deliverables/content?relativePath=${encodeURIComponent(relativePath)}`, {
      headers: { 'Authorization': 'Bearer ' + session.token },
    });
    if (!res.ok) return `(HTTP ${res.status})`;
    const body = await res.text();
    try { const j = JSON.parse(body); return j.data || j.Content || body; }
    catch { return body; }
  } catch (e) { return `(读取失败: ${e.message})`; }
}

main().catch(e => {
  console.error(e);
  writeEvidence('arch-skill-verify.json', { pass: false, error: e.message, steps });
  process.exit(1);
});
