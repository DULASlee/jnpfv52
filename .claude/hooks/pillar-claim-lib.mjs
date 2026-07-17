/**
 * 四大支柱 claim 校验（pillar-claim-v1）
 * 声称节点可审批前必须存在合法 .claude/pillar-claim-current.json
 */
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { dirname, join } from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));

export function getProjectRoot() {
  try {
    return execSync('git rev-parse --show-toplevel', {
      encoding: 'utf-8', stdio: 'pipe', timeout: 3000,
    }).trim().replace(/\\/g, '/');
  } catch { /* fall through */ }
  return join(__dirname, '..', '..').replace(/\\/g, '/');
}

export const CLAIM_REL = '.claude/pillar-claim-current.json';

const BUGFIX_ONLY_RE = /纠偏|表头|文案|待确认节|仅修复|只修|hotfix|typo|空壳表头/i;

/**
 * @returns {{ ok: boolean, errors: string[], claim?: object }}
 */
export function validatePillarClaim(claim) {
  const errors = [];
  if (!claim || typeof claim !== 'object') {
    return { ok: false, errors: ['claim 不是对象'] };
  }
  if (claim.schema !== 'pillar-claim-v1') {
    errors.push('schema 必须为 pillar-claim-v1');
  }
  if (!claim.node || String(claim.node).trim().length < 2) {
    errors.push('node（SG/阶段名）必填');
  }
  if (!claim.claimedAt) {
    errors.push('claimedAt 必填');
  }

  const p1 = claim.pillar1_business;
  if (!p1 || typeof p1 !== 'object') {
    errors.push('pillar1_business 必填');
  } else {
    if (!p1.capability || String(p1.capability).trim().length < 20) {
      errors.push('pillar1_business.capability 须≥20字，写清业务能力本体');
    }
    if (!p1.userAction || String(p1.userAction).trim().length < 8) {
      errors.push('pillar1_business.userAction 必填');
    }
    if (!Array.isArray(p1.deliverables) || p1.deliverables.length < 1) {
      errors.push('pillar1_business.deliverables 至少 1 项');
    }
    if (!Array.isArray(p1.evidence) || p1.evidence.length < 1) {
      errors.push('pillar1_business.evidence 至少 1 项（业务证据，非仅测试绿）');
    }
    if (p1.notBugfixOnly !== true) {
      errors.push('pillar1_business.notBugfixOnly 必须为 true（确认非纠偏顶替①）');
    }
    const blob = `${p1.capability} ${p1.userAction}`;
    if (BUGFIX_ONLY_RE.test(blob) && String(p1.capability).length < 40) {
      errors.push('pillar1 疑似纠偏项冒充①：请对照 §0.11 写业务能力本体');
    }
  }

  const p2 = claim.pillar2_data;
  if (!p2 || typeof p2 !== 'object') {
    errors.push('pillar2_data 必填');
  } else {
    if (!p2.writeModel) errors.push('pillar2_data.writeModel 必填');
    if (p2.tripleKeyOk !== true) errors.push('pillar2_data.tripleKeyOk 必须为 true');
    if (!Array.isArray(p2.evidence) || p2.evidence.length < 1) {
      errors.push('pillar2_data.evidence 至少 1 项');
    }
  }

  const p3 = claim.pillar3_legacy;
  if (!p3 || typeof p3 !== 'object') {
    errors.push('pillar3_legacy 必填');
  } else if (!p3.clearedOrNA || String(p3.clearedOrNA).trim().length < 4) {
    errors.push('pillar3_legacy.clearedOrNA 必填');
  }

  const p4 = claim.pillar4_xunit;
  if (!p4 || typeof p4 !== 'object') {
    errors.push('pillar4_xunit 必填');
  } else {
    if (!p4.command) errors.push('pillar4_xunit.command 必填');
    if (!p4.result) errors.push('pillar4_xunit.result 必填');
  }

  if (!claim.agentAttestation || !/确认|业务功能本体|非纠偏/.test(String(claim.agentAttestation))) {
    errors.push('agentAttestation 须明确确认①为业务功能本体、非纠偏顶替');
  }

  return { ok: errors.length === 0, errors, claim };
}

export function loadAndValidateClaim(root = getProjectRoot()) {
  const path = `${root}/${CLAIM_REL}`;
  if (!existsSync(path)) {
    return {
      ok: false,
      errors: [`缺少 ${CLAIM_REL}。声称可审批前按 .cursor/templates/four-pillars-checkpoint.md 填写`],
    };
  }
  let claim;
  try {
    claim = JSON.parse(readFileSync(path, 'utf8'));
  } catch (e) {
    return { ok: false, errors: [`${CLAIM_REL} JSON 无效: ${e.message}`] };
  }
  return validatePillarClaim(claim);
}

/** 是否处于「需要四支柱 claim」的节点工作态 */
export function requiresPillarClaim(root = getProjectRoot()) {
  const claimPath = `${root}/${CLAIM_REL}`;
  // 已写 claim 则始终校验（防止半成品胡乱声称）
  if (existsSync(claimPath)) return true;

  const wfPath = `${root}/.claude/workflow-state.json`;
  if (!existsSync(wfPath)) return false;
  try {
    const wf = JSON.parse(readFileSync(wfPath, 'utf8'));
    // pillarGateEnabled===false 总关闭；默认开启「按节点态」
    if (wf.pillarGateEnabled === false) return false;
    if (wf.awaitingNodeApproval === true) return true;
    if (wf.currentSg || wf.currentSG || wf.sg) return true;
    if (wf.phase === 'verify' || wf.phase === 'complete' || wf.phase === 'approval') return true;
    // 兼容旧字段：requirePillarClaim 仅在与节点态联用时生效
    if (wf.requirePillarClaim === true && (wf.awaitingNodeApproval || wf.currentSg || wf.currentSG)) {
      return true;
    }
  } catch { /* ignore */ }
  return false;
}

export function ensureWorkflowDefaults(root = getProjectRoot()) {
  const wfPath = `${root}/.claude/workflow-state.json`;
  const dir = `${root}/.claude`;
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  let wf = {};
  if (existsSync(wfPath)) {
    try { wf = JSON.parse(readFileSync(wfPath, 'utf8')); } catch { wf = {}; }
  }
  let dirty = false;
  if (wf.pillarGateEnabled === undefined) {
    wf.pillarGateEnabled = true;
    dirty = true;
  }
  if (wf.pillarClaimPath === undefined) {
    wf.pillarClaimPath = CLAIM_REL;
    dirty = true;
  }
  if (dirty) {
    writeFileSync(wfPath, JSON.stringify(wf, null, 2) + '\n', 'utf8');
  }
  return wf;
}

function main() {
  const root = getProjectRoot();
  const force = process.argv.includes('--force');
  if (!force && !requiresPillarClaim(root) && !existsSync(`${root}/${CLAIM_REL}`)) {
    console.log('pillar-claim-check: 当前未要求 claim（无 workflow 节点态 / 无 claim 文件），跳过');
    process.exit(0);
  }
  const result = loadAndValidateClaim(root);
  if (!result.ok) {
    console.error('BLOCKED: 四大支柱 claim 校验失败');
    for (const e of result.errors) console.error(`  - ${e}`);
    console.error('模板: .cursor/templates/four-pillars-checkpoint.md');
    process.exit(1);
  }
  console.log(`OK: pillar claim 有效 (node=${result.claim.node})`);
  process.exit(0);
}

const isDirect = process.argv[1] && /pillar-claim-(check|lib)\.mjs$/i.test(process.argv[1].replace(/\\/g, '/'));
if (isDirect && /pillar-claim-check\.mjs$/i.test(process.argv[1].replace(/\\/g, '/'))) main();
