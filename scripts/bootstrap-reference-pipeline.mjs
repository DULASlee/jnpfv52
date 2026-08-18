#!/usr/bin/env node

/**
 * 参考 Pipeline 自举脚本 (Polling-based)
 * ——————————————————————————————————————
 * 创建新 pipeline，跑完三轮需求分析编排器（每轮 skipAll 跳过澄清题），
 * 产出完整 02-requirement-spec.md（含 CTA + PM终评），输出 ID 供 E2E 使用。
 *
 * 设计原则：
 *   - 编排器 API 始终返回 { status: "running" }（后台异步执行），本脚本通过轮询
 *     IR snapshots/events 判断编排器进度。
 *   - 每轮：调编排器 → 轮询 ClarificationRequested snapshot → skipAll →
 *     等 snapshot stable → 下一轮。
 *   - 三轮完成后调编排器触发 auto-finalize（PM终评 + CTA）。
 *
 * 用法：
 *   node scripts/bootstrap-reference-pipeline.mjs [--name "项目名"] [--requirement "需求描述"]
 *
 * 环境变量：
 *   E2E_BOOTSTRAP_NAME         项目名（默认 "E2E 参考基准项目"）
 *   E2E_BOOTSTRAP_REQUIREMENT  需求描述（默认内置请假审批系统）
 */

import { apiRequest, isJnpfOk, jnpfData, login } from './lib/jnpf-auth.mjs';
import { createPipeline, log, warn } from './lib/phase-sup-api.mjs';

const NAME = process.env.E2E_BOOTSTRAP_NAME || 'E2E 参考基准项目';
const REQUIREMENT = process.env.E2E_BOOTSTRAP_REQUIREMENT || null;

// ─── 轮询参数 ──────────────────────────────────────────────────────────────────
const POLL_INTERVAL_MS = 3000;   // 3 秒心跳
const ROUND_TIMEOUT_MS = 600_000; // 每轮最多等 10 分钟（LLM 生成可能慢）
const FINALIZE_TIMEOUT_MS = 300_000; // 最终化最多等 5 分钟

// ─── 工具函数 ──────────────────────────────────────────────────────────────────

/**
 * 轮询直到 predicate 返回 truthy 值。
 * @returns {Promise<*>} predicate 返回的 truthy 值
 * @throws {Error} 超时
 */
async function pollUntil(predicate, label, timeoutMs = ROUND_TIMEOUT_MS, intervalMs = POLL_INTERVAL_MS) {
  const deadline = Date.now() + timeoutMs;
  let lastResult = null;
  while (Date.now() < deadline) {
    const result = await predicate();
    if (result) return result;
    lastResult = result;
    await new Promise(r => setTimeout(r, intervalMs));
  }
  throw new Error(`轮询超时: ${label} (${Math.round(timeoutMs / 1000)}s)，lastResult=${JSON.stringify(lastResult)}`);
}

/**
 * 获取 IR 事件列表。
 */
async function fetchEvents(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/events`, { session });
  const data = jnpfData(res);
  if (Array.isArray(data)) return data;
  if (Array.isArray(res.json)) return res.json;
  return data?.events || data?.items || [];
}

/**
 * 获取 IR 快照列表。
 */
async function fetchSnapshots(session, pipelineId) {
  const res = await apiRequest('GET', `/api/studio/ir/${pipelineId}/snapshots`, { session });
  const data = jnpfData(res);
  if (Array.isArray(data)) return data;
  if (Array.isArray(res.json)) return res.json;
  return [];
}

/**
 * 规范化字段名（API 返回 camelCase 或 PascalCase 不定）。
 */
function normField(obj, ...names) {
  for (const n of names) {
    if (obj && obj[n] !== undefined) return obj[n];
  }
  return undefined;
}

// ─── 编排器与澄清 API ────────────────────────────────────────────────────────

/**
 * 调用三轮编排器入口。API 始终返回 { runId, status: "running" }。
 */
async function callOrchestrator(session, pipelineId, body = {}) {
  log(`调用编排器 POST /api/studio/skills/requirement-analysis/${pipelineId}/run`);
  const res = await apiRequest(
    'POST',
    `/api/studio/skills/requirement-analysis/${pipelineId}/run`,
    { body, session, timeoutMs: 30_000 },
  );
  if (!isJnpfOk(res)) {
    throw new Error(`编排器调用失败: HTTP ${res.status} ${JSON.stringify(res.json)}`);
  }
  const data = jnpfData(res);
  log(`编排器已启动: runId=${data?.runId}, status=${data?.status}`);
  return data;
}

/**
 * 等待 Round N 的 ClarificationRequested snapshot 出现并提取 setId。
 *
 * 快照特征：
 *   - FragmentType == "IR1_Clarification"
 *   - FragmentId 包含 "requirement-analysis-round{N}"
 *   - Payload.setId 存在
 *
 * @returns {{ setId: string, snap: object }}
 */
async function waitForClarificationSnapshot(session, pipelineId, round) {
  const stageId = `requirement-analysis-round${round}`;
  log(`等待 Round ${round} ClarificationRequested snapshot (stage=${stageId})...`);

  return pollUntil(async () => {
    const snaps = await fetchSnapshots(session, pipelineId);

    for (const s of snaps) {
      const fragmentType = normField(s, 'fragmentType', 'FragmentType');
      const fragmentId = normField(s, 'fragmentId', 'FragmentId') || '';

      if (fragmentType !== 'IR1_Clarification') continue;
      if (!fragmentId.includes(stageId)) continue;

      // 提取 setId from Payload
      const payload = normField(s, 'payload', 'Payload');
      if (!payload) continue;

      let setId = null;
      if (typeof payload === 'object' && !Array.isArray(payload)) {
        setId = payload.setId || payload.SetId;
      } else if (typeof payload === 'string') {
        try { setId = JSON.parse(payload).setId; } catch { /* not JSON */ }
      }

      if (setId) {
        const version = normField(s, 'currentVersion', 'CurrentVersion');
        log(`  → 找到澄清快照: fragmentId=${fragmentId}, version=${version}`);
        return { setId, snap: s };
      }
    }
    return null;
  }, `Round ${round} ClarificationRequested`);
}

/**
 * skipAll 回答澄清题。
 * POST /api/studio/skills/clarification/{pipelineId}/answer
 *   body: { setId, skipAll: true }
 */
async function skipAllClarification(session, pipelineId, setId) {
  log(`skipAll 回答: setId=${setId.substring(0, 8)}...`);
  const res = await apiRequest(
    'POST',
    `/api/studio/skills/clarification/${pipelineId}/answer`,
    { body: { setId, skipAll: true }, session, timeoutMs: 30_000 },
  );
  if (!isJnpfOk(res)) {
    throw new Error(`skipAll 失败: HTTP ${res.status} ${JSON.stringify(res.json)}`);
  }
  const data = jnpfData(res);
  log(`skipAll 结果: status=${normField(data, 'status', 'Status')}, nextAction=${normField(data, 'nextAction', 'NextAction')}`);
  return data;
}

/**
 * 等待 Round N 的澄清快照变为 stable（StabilityState == "stable"）。
 */
async function waitForClarificationStable(session, pipelineId, round) {
  const stageId = `requirement-analysis-round${round}`;
  log(`等待 Round ${round} 澄清变为 stable...`);

  return pollUntil(async () => {
    const snaps = await fetchSnapshots(session, pipelineId);
    for (const s of snaps) {
      const fragmentType = normField(s, 'fragmentType', 'FragmentType');
      const fragmentId = normField(s, 'fragmentId', 'FragmentId') || '';
      const stability = normField(s, 'stabilityState', 'StabilityState');

      if (fragmentType !== 'IR1_Clarification') continue;
      if (!fragmentId.includes(stageId)) continue;
      if (stability !== 'stable') return null;

      log(`  → Round ${round} 澄清已 stable`);
      return s;
    }
    return null;
  }, `Round ${round} clarification stable`);
}

/**
 * 等待 AnalysisCompleted 事件。
 */
async function waitForAnalysisCompleted(session, pipelineId) {
  log('等待 AnalysisCompleted 事件...');

  return pollUntil(async () => {
    const events = await fetchEvents(session, pipelineId);
    for (const e of events) {
      const type = normField(e, 'eventType', 'EventType');
      if (type === 'AnalysisCompleted') {
        log('  → 找到 AnalysisCompleted 事件');
        return e;
      }
    }
    return null;
  }, 'AnalysisCompleted', FINALIZE_TIMEOUT_MS);
}

// ─── 验证 ──────────────────────────────────────────────────────────────────────

/**
 * 验证 pipeline 产出物是否具备 E2E 关键特征。
 * @returns {string[]} 问题列表，空 = 全部通过
 */
async function verifyPipeline(pipelineId, session) {
  const issues = [];

  // 1. 02-requirement-spec.md 含 CTA
  try {
    const specRes = await apiRequest(
      'GET',
      `/api/studio/pipeline/execute/${pipelineId}/deliverables/content?relativePath=02-requirement-spec.md`,
      { session, timeoutMs: 15_000 },
    );
    if (isJnpfOk(specRes)) {
      const specData = jnpfData(specRes);
      const content = specData?.content ?? specData ?? '';
      const contentStr = typeof content === 'object' ? JSON.stringify(content) : String(content);
      if (contentStr.includes('请你确认需求分析说明书')) {
        log('✅ CTA 文本存在于 02-requirement-spec.md');
      } else {
        issues.push('02-requirement-spec.md 缺少 CTA 固定文本（"请你确认需求分析说明书"）');
      }
    } else {
      issues.push('无法读取 02-requirement-spec.md');
    }
  } catch (e) {
    issues.push(`读取 02 异常: ${e.message}`);
  }

  // 2. IR 事件
  try {
    const events = await fetchEvents(session, pipelineId);
    const hasPmReviewed = events.some(e =>
      normField(e, 'eventType', 'EventType') === 'RequirementSpecPmReviewed'
    );
    if (hasPmReviewed) {
      log('✅ RequirementSpecPmReviewed 事件存在');
    } else {
      issues.push('IR 中未找到 RequirementSpecPmReviewed 事件');
    }

    const hasAnalysisCompleted = events.some(e =>
      normField(e, 'eventType', 'EventType') === 'AnalysisCompleted'
    );
    if (hasAnalysisCompleted) {
      log('✅ AnalysisCompleted 事件存在');
    } else {
      issues.push('IR 中未找到 AnalysisCompleted 事件');
    }
  } catch (e) {
    log(`⚠ IR 事件验证异常: ${e.message}（非阻塞）`);
  }

  return issues;
}

// ─── main ────────────────────────────────────────────────────────────────────

async function main() {
  log('═══════════════════════════════════════════════════');
  log('  参考 Pipeline Bootstrap (Polling-based v2)');
  log('═══════════════════════════════════════════════════');

  // ── Step 1: 登录 ──
  log('Step 1/5: 登录...');
  const session = await login();
  log(`  已登录: account=${session.account}`);

  // ── Step 2: 创建 pipeline ──
  log('Step 2/5: 创建 pipeline...');
  const pipelineId = await createPipeline(session, NAME, REQUIREMENT);
  log(`  ✅ Pipeline 已创建: ${pipelineId}`);

  // ── Step 3: 三轮编排 + skipAll ──
  log('Step 3/5: 运行三轮需求分析编排器...');

  for (let round = 1; round <= 3; round++) {
    log(`\n── Round ${round}/3 ──`);

    // 3a. 调用编排器（后台启动）
    await callOrchestrator(session, pipelineId);

    // 3b. 等待 ClarificationRequested snapshot 出现
    const { setId } = await waitForClarificationSnapshot(session, pipelineId, round);
    log(`  ✅ Round ${round} 澄清题已出`);

    // 3c. skipAll 回答
    await skipAllClarification(session, pipelineId, setId);

    // 3d. 等待 snapshot 变为 stable（确保编排器下一轮不会短路返回 awaiting-answer）
    await waitForClarificationStable(session, pipelineId, round);

    // 短暂睡眠让 semaphore 释放 + SSE 传播
    await new Promise(r => setTimeout(r, 2000));
  }

  // ── Step 4: 最终化（PM终评 + CTA）──
  log('\nStep 4/5: 触发最终化（三轮已稳定 → auto-finalize）...');

  // 再次调用编排器；DetermineCurrentRound 返回 4 → auto-finalize 路径
  await callOrchestrator(session, pipelineId);

  // 等待 AnalysisCompleted
  const completedEvent = await waitForAnalysisCompleted(session, pipelineId);
  log(`  ✅ AnalysisCompleted: eventId=${normField(completedEvent, 'eventId', 'EventId')}`);

  // ── Step 5: 验证 ──
  log('\nStep 5/5: 验证产出物...');
  let issues = await verifyPipeline(pipelineId, session);

  // 如果 PM终评未通过（CTA/PM事件缺失），尝试 ForceRefinalize
  if (issues.length > 0) {
    const hasCtaMissing = issues.some(i => i.includes('CTA'));
    const hasPmMissing = issues.some(i => i.includes('RequirementSpecPmReviewed'));
    if (hasCtaMissing || hasPmMissing) {
      log('\n⚠ 第一轮验证未通过，尝试 ForceRefinalize...');
      await callOrchestrator(session, pipelineId, {
        forceRefinalize: true,
        forceConfirm: true,
        forceReason: 'Bootstrap 参考 pipeline 强制完成 PM终评 + CTA',
      });
      await waitForAnalysisCompleted(session, pipelineId);
      issues = await verifyPipeline(pipelineId, session);
    }
  }

  // ── 输出 ──
  console.log('');
  console.log('═══════════════════════════════════════════════════');
  if (issues.length === 0) {
    console.log('  ✅ Bootstrap 成功！');
  } else {
    console.log('  ⚠ Bootstrap 完成，但有以下注意项：');
    for (const issue of issues) {
      console.log(`    - ${issue}`);
    }
  }
  console.log(`  Pipeline ID: ${pipelineId}`);
  console.log(`  使用方式:    E2E_PIPELINE_ID=${pipelineId} pnpm test:api`);
  console.log('═══════════════════════════════════════════════════');

  process.exit(issues.length > 0 ? 1 : 0);
}

main().catch(err => {
  console.error('[bootstrap] 致命错误:', err);
  process.exit(2);
});
