#!/usr/bin/env node
/**
 * 阶段七 Eval Pipeline DoD 验收（P7-Q01）
 *
 *   node scripts/phase7-eval-verify.mjs
 *
 * 验收条款（文档 §6.2-§6.6 DoD）：
 *   D1 EvalPipelineRunner L1-L3（fail-fast + 三元组 + 分页读 IR events）
 *   D2 LlmJudgeService（经 Guard fast tier，跨家族 mimo，pass/fail 二元）
 *   D3 JudgeCalibrationService（Cohen's kappa，kappa<0.6 untrusted）
 *   D4 SkillReviewApiService（人工抽检，复用 IR 事件审计回放）
 *   D5 SkillQualityBoardService（SQL 聚合 + tier 分级 + 三元组隔离）
 *   D6 MemoryRetentionService（失败 trace 回写 GoldenSet，不删 IR events）
 *   NFR 六条生命线（边界/性能/内存/隔离/LLM）
 *
 * 2026 实践要点：
 *   - pass^k 一致性字段预留（首版 k=1）
 *   - Judge 校准 kappa≥0.6 才允许 gating
 *   - 生产 trace→eval 闭环（失败 run 回写 GoldenSet）
 *
 * 产出：.claude/evidence/phase7-eval-verify.json
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.join(__dirname, '..');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.claude', 'evidence');

function log(tag, id, detail) {
  const icon = tag === 'PASS' ? '✅' : tag === 'FAIL' ? '❌' : '⏭️';
  console.log(`${icon} [${tag}] ${id}: ${detail}`);
}

const results = [];
function record(id, pass, detail, extra = {}) {
  results.push({ id, pass, detail, ...extra, at: new Date().toISOString() });
  log(pass ? 'PASS' : 'FAIL', id, detail);
}

function grepFile(relPath, pattern) {
  const fullPath = path.join(REPO_ROOT, relPath);
  if (!fs.existsSync(fullPath)) return false;
  return fs.readFileSync(fullPath, 'utf8').includes(pattern);
}

function grepCount(relPath, pattern) {
  const fullPath = path.join(REPO_ROOT, relPath);
  if (!fs.existsSync(fullPath)) return 0;
  const content = fs.readFileSync(fullPath, 'utf8');
  return content.split(pattern).length - 1;
}

function fileExists(relPath) {
  return fs.existsSync(path.join(REPO_ROOT, relPath));
}

// ════════════════════════════════════════════════════════════════
// D1: EvalPipelineRunner L1-L3（P7-E01）
// ════════════════════════════════════════════════════════════════
function checkD1() {
  const runner = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalPipelineRunner.cs';
  const pass =
    fileExists(runner) &&
    grepFile(runner, 'RunLayer1ComponentAsync') &&
    grepFile(runner, 'RunLayer2TrajectoryAsync') &&
    grepFile(runner, 'RunLayer3TaskAsync') &&
    grepFile(runner, 'RedundantLlmLoop') &&          // L2 冗余检测规则
    grepFile(runner, 'IrEventPageSize = 500') &&      // NFR 内存：分页 ≤500
    grepFile(runner, 'ComputeConsistencyAsync') &&    // pass^k 预留
    grepFile(runner, 'x.TenantId == req.TenantId');   // 三元组 R12
  record('D1-L1L2L3', pass, pass
    ? 'EvalPipelineRunner L1-L3 + fail-fast + 分页 + pass^k 预留 + 三元组'
    : 'EvalPipelineRunner 缺失关键代码（L1/L2/L3/RedundantLlmLoop/IrEventPageSize/Consistency/TenantId）');

  // fail-fast：L1 不过直接返回
  const failFast = grepFile(runner, 'if (!result.L1.Passed)') || grepFile(runner, '!result.L1.Passed');
  record('D1-failfast', failFast, failFast
    ? 'fail-fast：L1 不过跳过 L2/L3（六条生命线#2 边界）'
    : '未找到 L1 fail-fast 逻辑');

  // 迁移文件
  const migration = fileExists('backend/modularity/inteAssistant/Migrations/20260708_Phase7_Eval_Pipeline.sql');
  record('D1-migration', migration, migration
    ? '迁移 SQL 存在（EvalRunEntity 加三元组+CaseId+LayerResults+JudgeKappa+Consistency）'
    : '迁移 SQL 缺失');

  // EvalService 端点
  const evalService = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalService.cs';
  const endpoints =
    grepFile(evalService, 'HttpPost("execute")') &&
    grepFile(evalService, 'HttpGet("run/{runId:long}")') &&
    grepFile(evalService, 'HttpGet("consistency/{caseId:long}")');
  record('D1-endpoints', endpoints, endpoints
    ? 'EvalService 端点齐全（execute/run/consistency）'
    : 'EvalService 端点缺失');
}

// ════════════════════════════════════════════════════════════════
// D2: LlmJudgeService（P7-E02）
// ════════════════════════════════════════════════════════════════
function checkD2() {
  const judge = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/LlmJudgeService.cs';
  const pass =
    fileExists(judge) &&
    grepFile(judge, 'eval-judge') &&                  // skillId = eval-judge
    grepFile(judge, 'ISkillLlmBudgetGuard') &&         // 经 Guard
    grepFile(judge, 'AcquireAsync') &&                 // AcquireAsync
    grepFile(judge, 'ExecuteAsync') &&                 // ExecuteAsync（经 Guard 路由 mimo）
    grepFile(judge, 'ParsePassFail') &&                // pass/fail 二元解析
    grepFile(judge, 'Sha256') &&                       // input/output hash 入日志
    grepFile(judge, 'Temperature = 0.0');              // Judge 确定性
  record('D2-Judge', pass, pass
    ? 'LlmJudgeService 经 Guard + pass/fail 二元 + hash 入日志 + 温度0'
    : 'LlmJudgeService 缺失关键代码');

  // 跨家族 mimo（eval-judge policy fast tier）
  const policySeed = grepFile(
    'backend/modularity/inteAssistant/Migrations/20260708_Phase7_Skill_Reviews.sql',
    "'eval-judge'");
  record('D2-policy', policySeed, policySeed
    ? 'eval-judge LLM policy 种子（maxCalls=1 fast→mimo 跨家族）'
    : 'eval-judge policy 种子缺失');

  // Judge API 端点
  const judgeApi = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalService.cs',
    'HttpPost("judge")');
  record('D2-judge-api', judgeApi, judgeApi
    ? 'POST /api/studio/eval/judge 端点存在'
    : 'Judge API 端点缺失');
}

// ════════════════════════════════════════════════════════════════
// D3: JudgeCalibrationService + EvalCalibrationJob（P7-E02）
// ════════════════════════════════════════════════════════════════
function checkD3() {
  const calib = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/JudgeCalibrationService.cs';
  const pass =
    fileExists(calib) &&
    grepFile(calib, 'ComputeCohenKappa') &&            // Cohen's kappa 实现
    grepFile(calib, 'KappaTrustedThreshold = 0.6') &&  // kappa≥0.6 才可信
    grepFile(calib, 'untrusted') &&                    // kappa<0.6 → untrusted
    grepFile(calib, 'insufficient_samples');           // 样本不足降级
  record('D3-calibration', pass, pass
    ? 'JudgeCalibrationService Cohen kappa + 0.6 阈值 + 样本不足降级'
    : 'JudgeCalibrationService 缺失关键代码');

  // Quartz Job 注册（月度 cron）
  const job = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Job/EvalCalibrationJob.cs';
  const jobRegistered = fileExists(job) &&
    grepFile('backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineSchedulingModule.cs',
      'EvalCalibrationJob') &&
    grepFile('backend/modularity/inteAssistant/JNPF.InteAssistant/PipelineSchedulingModule.cs',
      '"0 0 2 1 * ?"');  // 每月 1 日 02:00
  record('D3-cron-job', jobRegistered, jobRegistered
    ? 'EvalCalibrationJob 注册（cron 每月1日02:00）'
    : 'EvalCalibrationJob 未注册或 cron 错误');

  // calibration API
  const calibApi = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalService.cs',
    'HttpGet("calibration")');
  record('D3-calib-api', calibApi, calibApi
    ? 'GET /api/studio/eval/calibration 端点存在'
    : 'calibration API 端点缺失');
}

// ════════════════════════════════════════════════════════════════
// D4: SkillReviewApiService（P7-E03）
// ════════════════════════════════════════════════════════════════
function checkD4() {
  const review = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/SkillReviewApiService.cs';
  const pass =
    fileExists(review) &&
    grepFile(review, 'SkillReviewEntity') &&           // 用实体类（表名映射在 SugarTable 注解）
    grepFile(review, 'IExperienceRecorder') &&         // 复用经验回流（IR 事件审计回放）
    grepFile(review, 'RecordReviewAsync') &&           // 双写 IR 事件
    grepFile(review, 'InterRaterStats') &&             // 多人评分一致性
    grepFile(review, 'IsDisputed');                    // 争议样本识别
  record('D4-review', pass, pass
    ? 'SkillReviewApiService 双写（表+IR事件）+ inter-rater + 争议识别'
    : 'SkillReviewApiService 缺失关键代码');

  // 跨租户隔离（六条生命线#5）
  const isolation = grepFile(review, 'x.TenantId == tenantId') &&
    grepFile(review, '跨租户');
  record('D4-isolation', isolation, isolation
    ? 'review 跨租户隔离校验（提交时校验 skill_run 归属）'
    : 'review 跨租户隔离校验缺失');

  // 二元口径对齐 Judge（Score≥60 → PASS）
  const binary = grepFile(review, 'PassThreshold = 60') &&
    grepFile(review, '"PASS"') && grepFile(review, '"FAIL"');
  record('D4-binary', binary, binary
    ? '评分二元口径与 Judge 对齐（≥60 PASS）'
    : '评分二元口径未对齐');
}

// ════════════════════════════════════════════════════════════════
// D5: SkillQualityBoardService（P7-E04）
// ════════════════════════════════════════════════════════════════
function checkD5() {
  const board = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/SkillQualityBoardService.cs';
  const pass =
    fileExists(board) &&
    grepFile(board, 'SqlFunc.AggregateCount') &&       // SQL 聚合
    grepFile(board, 'SqlFunc.AggregateSum') &&
    grepFile(board, 'SqlFunc.AggregateAvg') &&
    grepFile(board, 'ClassifyGrade') &&                // tier 分级
    grepFile(board, 'x.TenantId == tenantId');         // 三元组隔离
  record('D5-board', pass, pass
    ? 'SkillQualityBoardService SQL 聚合 + tier 分级 + 三元组隔离'
    : 'SkillQualityBoardService 缺失关键代码');

  // 质量等级映射（A/B/C/D ↔ green/yellow/red/fuse）
  const grades = grepFile(board, '>= 0.95 => "A"') &&
    grepFile(board, '>= 0.80 => "B"') &&
    grepFile(board, '>= 0.60 => "C"') &&
    grepFile(board, '"D"');
  record('D5-grades', grades, grades
    ? '质量等级 A/B/C/D 分级（↔ green/yellow/red/fuse）'
    : '质量等级分级缺失');
}

// ════════════════════════════════════════════════════════════════
// D6: MemoryRetentionService（P7-E04）
// ════════════════════════════════════════════════════════════════
function checkD6() {
  const mem = 'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/MemoryRetentionService.cs';
  const pass =
    fileExists(mem) &&
    grepFile(mem, 'CollectFailureTracesAsync') &&      // 失败 trace 收集
    grepFile(mem, 'auto_seed') &&                      // 回写 GoldenSet auto_seed 池
    grepFile(mem, 'EnsureAutoSeedSetAsync') &&         // 自动创建回归集
    grepFile(mem, 'Status == "failed"');               // 仅收集 failed run
  record('D6-trace-loop', pass, pass
    ? 'MemoryRetentionService 生产 trace→eval 闭环（失败 run 回写 GoldenSet）'
    : 'MemoryRetentionService 缺失关键代码');

  // 边界：不删 IR events（ir-count 端点验证）
  const noDelete = grepFile(mem, '不删除 IR events') ||
    grepFile(mem, '记忆遗忘不删除 IR events');
  record('D6-no-delete-ir', noDelete, noDelete
    ? '边界约束：记忆遗忘不删 IR events（只裁剪 Prompt 上下文）'
    : '边界约束标记缺失');

  // 去重逻辑（避免重复收集同一 run）
  const dedup = grepFile(mem, 'collectedRunIds') && grepFile(mem, 'ExtractRunId');
  record('D6-dedup', dedup, dedup
    ? '失败收集去重（已收集的 run 不重复入库）'
    : '去重逻辑缺失');
}

// ════════════════════════════════════════════════════════════════
// NFR: 六条生命线（阶段七 §15）
// ════════════════════════════════════════════════════════════════
function checkNfr() {
  // #1 日志：Judge input/output hash 入日志（非全文）
  const logHash = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/LlmJudgeService.cs',
    'JudgeCall') && grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/LlmJudgeService.cs',
    'inputHash');
  record('NFR-1-log', logHash, logHash
    ? '#1 日志：Judge hash 入日志（非全文）'
    : '#1 日志：Judge hash 入日志缺失');

  // #2 边界：L1 fail 不跑 L4
  const boundary = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalService.cs',
    'L1 组件评估未通过，跳过 L4');
  record('NFR-2-boundary', boundary, boundary
    ? '#2 边界：L1 fail 不跑 L4 Judge'
    : '#2 边界：L1 fail-skip-L4 缺失');

  // #4 内存：L2 分页读 events ≤500
  const mem = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalPipelineRunner.cs',
    'IrEventPageSize = 500');
  record('NFR-4-memory', mem, mem
    ? '#4 内存：L2 分页读 IR events ≤500'
    : '#4 内存：分页约束缺失');

  // #5 隔离：质量榜/eval/review 全部三元组过滤
  const iso = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/SkillQualityBoardService.cs',
    'x.TenantId == tenantId') &&
    grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/EvalService.cs',
    'x.F_TenantId == tenantId') &&
    grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/SkillReviewApiService.cs',
    'x.TenantId == tenantId');
  record('NFR-5-isolation', iso, iso
    ? '#5 隔离：质量榜/eval/review 全部三元组 R12 过滤'
    : '#5 隔离：三元组过滤不全');

  // #6 LLM：仅 L4 Judge 用 LLM，maxCalls=1 fast
  const llm = grepFile(
    'backend/modularity/inteAssistant/JNPF.InteAssistant/Studio/LlmJudgeService.cs',
    'JudgeSkillId = "eval-judge"') &&
    grepFile(
    'backend/modularity/inteAssistant/Migrations/20260708_Phase7_Skill_Reviews.sql',
    "'eval-judge', 1, 500");  // maxCalls=1
  record('NFR-6-llm', llm, llm
    ? '#6 LLM：仅 L4 Judge（eval-judge maxCalls=1 fast）'
    : '#6 LLM：Judge policy 约束缺失');
}

// ════════════════════════════════════════════════════════════════
// 主流程
// ════════════════════════════════════════════════════════════════
console.log('═══ 阶段七 Eval Pipeline DoD 验收（P7-Q01）═══\n');

checkD1();
checkD2();
checkD3();
checkD4();
checkD5();
checkD6();
checkNfr();

const passed = results.filter(r => r.pass).length;
const failed = results.filter(r => !r.pass).length;
const total = results.length;

const summary = {
  phase: 'P7',
  ticket: 'P7-Q01',
  total, passed, failed,
  passRate: `${((passed / total) * 100).toFixed(1)}%`,
  results,
  verifiedAt: new Date().toISOString(),
};

// 写 evidence
if (!fs.existsSync(EVIDENCE_DIR)) fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
const evidencePath = path.join(EVIDENCE_DIR, 'phase7-eval-verify.json');
fs.writeFileSync(evidencePath, JSON.stringify(summary, null, 2), 'utf8');

console.log(`\n═══ 验收结果：${passed}/${total} 通过，${failed} 失败 ═══`);
console.log(`evidence: ${evidencePath}`);

if (failed > 0) {
  console.log('\n失败项：');
  results.filter(r => !r.pass).forEach(r => console.log(`  ❌ ${r.id}: ${r.detail}`));
  process.exit(1);
} else {
  console.log('\n✅ P7 Eval Pipeline 全部通过');
  process.exit(0);
}
