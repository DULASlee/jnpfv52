#!/usr/bin/env node
/**
 * 修复 pipeline 311 IR3_GeneratedCode stability 状态：
 * 注入 CodeGeneratedStablePromoted 事件让 IrProjectionEngine 把 fragment 标 stable
 */
import { login, apiRequest } from './lib/jnpf-auth.mjs';

const PIPELINE_ID = 311;
const FRAGMENT_ID = 'codegen:311';

async function main() {
  const session = await login();

  // 注入 CodeGeneratedStablePromoted 事件（走 AppendAsync → 触发 projection）
  const res = await apiRequest('POST', `api/studio/ir/${PIPELINE_ID}/simulate`, {
    session,
    body: {
      eventType: 'CodeGeneratedStablePromoted',
      fragmentId: FRAGMENT_ID,
      fragmentType: 'IR3_GeneratedCode',
      fragmentVersion: 3,
      payload: JSON.stringify({
        projectId: String(PIPELINE_ID),
        pipelineId: PIPELINE_ID,
        fragmentId: FRAGMENT_ID,
        source: 'manual-fix-after-misordered-codegen',
        promotedAt: new Date().toISOString(),
      }),
    },
  });

  console.log('simulate response:', JSON.stringify(res.json, null, 2));

  if (res.status >= 300) {
    console.error('FAIL');
    process.exit(1);
  }

  // 验证 snapshot
  const snapRes = await apiRequest('GET', `api/studio/ir/${PIPELINE_ID}/snapshots`, { session });
  const snaps = snapRes.json?.data || [];
  const codegenSnap = snaps.find(s => (s.fragmentType || s.FragmentType) === 'IR3_GeneratedCode');
  console.log('\nIR3_GeneratedCode after fix:');
  console.log('  stabilityState:', codegenSnap?.stabilityState || codegenSnap?.StabilityState);
  console.log('  currentVersion:', codegenSnap?.currentVersion || codegenSnap?.CurrentVersion);
}

main().catch(e => {
  console.error('FAIL:', e.message);
  process.exit(1);
});
