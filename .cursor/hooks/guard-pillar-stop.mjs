#!/usr/bin/env node
/**
 * Cursor stop hook — 节点审批态下强制四大支柱 claim
 * 若 requirePillarClaim / awaitingNodeApproval / currentSg 且 claim 无效 → followup 要求补齐
 */
import {
  loadAndValidateClaim,
  requiresPillarClaim,
  getProjectRoot,
  ensureWorkflowDefaults,
  CLAIM_REL,
} from '../../.claude/hooks/pillar-claim-lib.mjs';

const root = getProjectRoot();
ensureWorkflowDefaults(root);

if (!requiresPillarClaim(root)) {
  process.stdout.write(JSON.stringify({}));
  process.exit(0);
}

const result = loadAndValidateClaim(root);
if (result.ok) {
  process.stdout.write(JSON.stringify({}));
  process.exit(0);
}

const msg = [
  '四大支柱硬门：当前处于节点审批态，但 claim 无效或缺失。',
  ...result.errors.map((e) => `- ${e}`),
  `请按 .cursor/templates/four-pillars-checkpoint.md 写入 ${CLAIM_REL}，然后运行:`,
  'node .claude/hooks/pillar-claim-check.mjs --force',
  '① 必须写业务功能本体（对照 §0.11），禁止纠偏/单测绿顶替。补齐前不得声称「可审批」。',
].join('\n');

process.stdout.write(JSON.stringify({
  followup_message: msg,
}));
process.exit(0);
