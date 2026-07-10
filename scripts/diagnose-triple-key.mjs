#!/usr/bin/env node
/**
 * 三元组健康检查（30 号 W4-T5）
 * 用法：node scripts/diagnose-triple-key.mjs
 */
import { login, pick, apiRequest, jnpfData } from './lib/jnpf-auth.mjs';

async function main() {
  const session = await login();
  const listRes = await apiRequest('GET', '/api/studio/pipeline/execute/list?pageIndex=0&pageSize=50', { session });
  const list = jnpfData(listRes) ?? listRes;
  const items = Array.isArray(list) ? list : pick(list, 'data', 'Data') || [];
  console.log(`pipelines visible to current user: ${items.length}`);

  let nonGreenfield = 0;
  for (const p of items.slice(0, 20)) {
    const id = pick(p, 'id', 'Id');
    if (!id) continue;
    try {
      const detailRes = await apiRequest('GET', `/api/studio/pipeline/execute/${id}`, { session });
      const d = jnpfData(detailRes) ?? detailRes;
      const workMode = pick(d, 'workMode', 'WorkMode') || 'greenfield';
      const projectId = pick(d, 'projectId', 'ProjectId');
      const pipelineId = String(pick(d, 'id', 'Id') || id);
      if (String(workMode).toLowerCase() !== 'greenfield') {
        nonGreenfield++;
        const ok = projectId && String(projectId) !== pipelineId;
        console.log(`  [${workMode}] pipeline=${pipelineId} projectId=${projectId} decoupled=${ok}`);
      }
    } catch (e) {
      console.warn(`  skip ${id}: ${e.message}`);
    }
  }

  console.log(`non-greenfield sampled: ${nonGreenfield}`);
  console.log('OK: diagnose-triple-key finished (list isolation + sample workMode)');
  process.exit(0);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
