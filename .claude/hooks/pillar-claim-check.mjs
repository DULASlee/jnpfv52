#!/usr/bin/env node
/**
 * CLI：校验 .claude/pillar-claim-current.json
 *   node .claude/hooks/pillar-claim-check.mjs
 *   node .claude/hooks/pillar-claim-check.mjs --force   # 强制校验（文件必须存在且合法）
 */
import { loadAndValidateClaim, requiresPillarClaim, getProjectRoot, CLAIM_REL } from './pillar-claim-lib.mjs';
import { existsSync } from 'fs';

const root = getProjectRoot();
const force = process.argv.includes('--force');

if (force) {
  const result = loadAndValidateClaim(root);
  if (!result.ok) {
    console.error('BLOCKED: 四大支柱 claim 校验失败 (--force)');
    for (const e of result.errors) console.error(`  - ${e}`);
    process.exit(1);
  }
  console.log(`OK: pillar claim 有效 (node=${result.claim.node})`);
  process.exit(0);
}

if (!requiresPillarClaim(root) && !existsSync(`${root}/${CLAIM_REL}`)) {
  console.log('pillar-claim-check: 跳过（未处于节点审批态）');
  process.exit(0);
}

const result = loadAndValidateClaim(root);
if (!result.ok) {
  console.error('BLOCKED: 四大支柱 claim 校验失败');
  for (const e of result.errors) console.error(`  - ${e}`);
  console.error(`填写: ${CLAIM_REL} ← .cursor/templates/four-pillars-checkpoint.md`);
  process.exit(1);
}
console.log(`OK: pillar claim 有效 (node=${result.claim.node})`);
process.exit(0);
