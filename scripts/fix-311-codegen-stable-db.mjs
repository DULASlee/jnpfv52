#!/usr/bin/env node
/**
 * 修复 pipeline 311 IR3_GeneratedCode 状态投影不一致
 * （CodeGenerated 事件在 StablePromoted 之后写入，导致 stable 被降级回 draft）
 */
import { runSqlQuery } from './lib/jnpf-db.mjs';

const PIPELINE_ID = 311;

const updateSql = `
UPDATE ai_ir_fragment_snapshots
SET F_StabilityState = N'stable',
    F_CurrentVersion = 3,
    F_UpdatedAt = SYSUTCDATETIME()
WHERE F_ProjectId = N'${PIPELINE_ID}'
  AND F_FragmentId = N'codegen:${PIPELINE_ID}'
  AND F_FragmentType = N'IR3_GeneratedCode'
  AND F_DeleteMark = 0;
`;

console.log('[fix-311] Updating IR3_GeneratedCode → stable...');
const updateOut = runSqlQuery(updateSql);
console.log('[fix-311] update result:', updateOut || '(no rows reported)');

const verifySql = `
SET NOCOUNT ON;
SELECT F_FragmentId, F_FragmentType, F_StabilityState, F_CurrentVersion, F_UpdatedAt
FROM ai_ir_fragment_snapshots
WHERE F_ProjectId = N'${PIPELINE_ID}'
  AND F_FragmentId = N'codegen:${PIPELINE_ID}'
  AND F_DeleteMark = 0;
`;
console.log('\n[fix-311] Verifying...');
const verifyOut = runSqlQuery(verifySql);
console.log(verifyOut);
