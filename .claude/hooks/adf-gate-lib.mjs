/**
 * ADF 写入门控 — P0–P3 禁止写业务源码
 */
import { existsSync, readFileSync } from 'fs';
import { execSync } from 'child_process';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

export function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch {
    return join(__dirname, '..', '..').replace(/\\/g, '/');
  }
}

const LOCKED = new Set(['P0', 'P1', 'P2', 'P3']);

/** 锁定期间仍允许写入的路径 */
export function isAdfExemptPath(filePath) {
  const p = (filePath || '').replace(/\\/g, '/');
  if (!p) return true;
  const allow = [
    /(^|\/)\.claude\//,
    /(^|\/)\.cursor\//,
    /(^|\/)docs\//,
    /(^|\/)openspec\//,
    /(^|\/)\.zcode\//,
    /\.md$/i,
    /\.mdc$/i,
    /\.json$/i,
    /\.yaml$/i,
    /\.yml$/i,
    /four-pillars-checkpoint/,
    /adf-(architecture|patterns|contracts)/,
    /task-kickoff/,
    /pillar-claim/,
    /workflow-state/,
  ];
  return allow.some((re) => re.test(p));
}

/** 业务源码（锁定时应拦） */
export function isAdfBusinessSource(filePath) {
  const p = (filePath || '').replace(/\\/g, '/');
  if (isAdfExemptPath(p)) return false;
  const biz = [
    /(^|\/)backend\/.*\.cs$/i,
    /(^|\/)jnpf-web-vue3\/src\/.*\.(vue|ts|tsx|js)$/i,
    /(^|\/)jnpf-web-datascreen\/src\/.*\.(vue|ts|tsx|js)$/i,
    /(^|\/)jnpf-app-vue3\/.*\.(vue|ts|js)$/i,
    /(^|\/)sa-service\/(?!.*node_modules).*\.(ts|js)$/i,
  ];
  return biz.some((re) => re.test(p));
}

export function loadWorkflowState(root = getProjectRoot()) {
  const wfPath = `${root}/.claude/workflow-state.json`;
  if (!existsSync(wfPath)) return {};
  try {
    return JSON.parse(readFileSync(wfPath, 'utf8'));
  } catch {
    return {};
  }
}

/**
 * 有效阶段：
 * - adfGateEnabled===false → null（关闭）
 * - adfPhase P4/exempt → 放行
 * - adfPhase P0–P3 → 锁定
 * - currentSg 已设且未给 phase → 视为 P0（进 SG 即锁码）
 * - 否则 null（日常不锁）
 */
export function getEffectiveAdfPhase(wf = loadWorkflowState()) {
  if (wf.adfGateEnabled === false) return null;
  const phase = wf.adfPhase ?? null;
  if (phase === 'P4' || phase === 'exempt') return phase;
  if (LOCKED.has(phase)) return phase;
  if (wf.currentSg || wf.currentSG || wf.sg) return 'P0';
  return null;
}

/**
 * @returns {{ block: boolean, phase: string|null, reason?: string }}
 */
export function checkAdfWrite(filePath, root = getProjectRoot(), wfOverride = null) {
  const wf = wfOverride ?? loadWorkflowState(root);
  const phase = getEffectiveAdfPhase(wf);
  if (!phase || phase === 'P4' || phase === 'exempt') {
    return { block: false, phase };
  }
  if (!isAdfBusinessSource(filePath)) {
    return { block: false, phase };
  }
  return {
    block: true,
    phase,
    reason:
      `ADF 写入锁：adfPhase=${phase}，禁止写业务源码 ${filePath}。` +
      `先完成 P1→P2→P3 并等用户「继续」，再把 workflow-state.json 的 adfPhase 设为 P4；` +
      `B 级设 adfPhase=exempt 并填写 adfExemptReason。`,
  };
}
